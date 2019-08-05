using System;
using System.Linq;
using System.Windows.Navigation;
using Microsoft.VisualStudio.Shell;

//------------------------------------------------------------------------------
// <copyright file="PreviewWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE80;

namespace DocPreview
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Web.Script.Serialization;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Interaction logic for PreviewWindowControl.
    /// </summary>
    public partial class PreviewWindowControl : UserControl
    {
        Config config = Config.Load();

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewWindowControl"/> class.
        /// </summary>
        public PreviewWindowControl()
        {
            this.InitializeComponent();

            AutoRefresh.IsChecked = config.AutoRefresh;

            if (config.DefaultTheme)
                DefaultTheme_Click(null, null);
            else
                DarkTheme_Click(null, null);

            Browser.Navigating += Browser_Navigating;
            Browser.LoadCompleted += Browser_LoadCompleted;
            Loaded += MainWindow_Loaded;
            Dispatcher.ShutdownStarted += (s, e) => config.Save();
            PreviewWindowPackage.OnLineChanged = AutoRefreshPreview;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            nextNavCanceled = false;
            if (XmlDocumentation.DocPreview.IsVersionFirstRun && !XmlDocumentation.DocPreview.IsFirstEverRun)
                ShowReleaseNotes();
            else
                Browser.NavigateToString(initialPrompt.Value);
        }

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            nextNavCanceled = true;
        }

        bool nextNavCanceled = false;

        private void Browser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            e.Cancel = nextNavCanceled;

            if (e.Uri != null)
            {
                bool isExternal = e.Uri.AbsoluteUri.StartsWith("http://external_link/");
                bool isInternal = e.Uri.AbsoluteUri.StartsWith("http://internal_link/");

                if (isInternal || isExternal)
                {
                    string version = this.Version.ToString();

                    string uri = e.Uri.AbsoluteUri
                                      .Replace("internal_link/", "")
                                      .Replace("external_link/", "")
                                      .Replace("{$version}", version)
                                      .Replace("%7B$version%7D", version);
                    if (isInternal)
                        Navigate(uri);
                    else
                        try
                        {
                            System.Diagnostics.Process.Start(uri);
                        }
                        catch { }
                }
            }
        }

        int lastPreviewLineNumber = -1;
        string lastPreviewFileName = null;
        string lastPreviewAllText = null;

        void AutoRefreshPreview()
        {
            if (AutoRefresh.IsChecked.Value)
                RefreshPreview(false);
        }

        bool showingCannotNavigateContent = false;
        bool protectDisplayedProductInfo = false;

        Lazy<string> initialPrompt = new Lazy<string>(() => XmlDocumentation.DocPreview.GenerateErrorHtml(@"<span style='font-style: italic;'><br>
<span style='color: red;'> Place cursor/caret at the C# XML
documentation comment region and click 'Refresh' icon. </span></span><br>"));

        Lazy<string> errorContent = new Lazy<string>(() => XmlDocumentation.DocPreview.GenerateErrorHtml(""));

        string DecorateError(string error)
        {
            return $@"<span style='font-style: italic;'><br>
<span style='color: red;'> {error} </span></span><br>";
        }

        void Navigate(string url)
        {
            nextNavCanceled = false;
            Browser.Navigate(url);
        }

        void ShowPreview(string content)
        {
            if (content.HasText())
            {
                protectDisplayedProductInfo =
                showingCannotNavigateContent = false;

                nextNavCanceled = false;
                Browser.NavigateToString(content);
            }
            else
            {
                if (!showingCannotNavigateContent && !protectDisplayedProductInfo)
                {
                    showingCannotNavigateContent = true;
                    nextNavCanceled = false;
                    Browser.NavigateToString(errorContent.Value);
                }
            }
        }

        void RefreshPreview(bool force = false)
        {
            try
            {
                if (force)
                    protectDisplayedProductInfo = false;

                IWpfTextView textView = Global.GetTextView();

                string language = textView.TextBuffer.ContentType.TypeName;

                if (textView != null && (language == "CSharp" || language == "F#" || language == "C/C++" || language == "Basic"))
                {
                    ITextSnapshot snapshot = textView.TextSnapshot;

                    if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                        return;

                    if (!textView.Selection.IsEmpty)
                        return;

                    DTE2 dte = Global.GetDTE2();
                    string fileName = dte.ActiveDocument.FullName;
                    int caretLineNumber = snapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition);
                    string code = snapshot.GetText();

                    if (force || (fileName != lastPreviewFileName || caretLineNumber != lastPreviewLineNumber || code != lastPreviewAllText))
                    {
                        lastPreviewFileName = fileName;
                        lastPreviewLineNumber = caretLineNumber;
                        lastPreviewAllText = code;

                        Parser.Result result = Parser.FindMemberDocumentation(code, caretLineNumber + 1, language);
                        var html = XmlDocumentation.DocPreview
                                                   .GenerateHtml(result.MemberTitle,
                                                                 result.MemberDefinition,
                                                                 result.XmlDocumentation);
                        if (language == "C/C++")
                            html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>C++</th>");
                        else if (language == "Basic")
                            html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>VB.NET</th>");
                        else if (language == "F#")
                            html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>F#</th>");

                        ShowPreview(html);
                    }
                }
            }
            catch (System.Exception e)
            {
                //just ignore the errors
                var html = XmlDocumentation.DocPreview.GenerateErrorHtml(DecorateError(e.Message));
                ShowPreview(html);
            }
        }

        string GeneratePreviewForAll()
        {
            try
            {
                IWpfTextView textView = Global.GetTextView();

                string language = textView.TextBuffer.ContentType.TypeName;

                if (textView != null && (language == "CSharp" || language == "F#" || language == "FSharp" || language == "C/C++"))
                {
                    ITextSnapshot snapshot = textView.TextSnapshot;

                    if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                        return null;

                    if (!textView.Selection.IsEmpty)
                        return null;

                    DTE2 dte = Global.GetDTE2();
                    string fileName = dte.ActiveDocument.FullName;
                    string code = snapshot.GetText();

                    var result = Parser.FindAllDocumentation(code, language).ToArray();

                    var content = result.Select(r => XmlDocumentation.DocPreview
                                                                     .GenerateRawHtml(r.MemberTitle,
                                                                                      r.MemberDefinition,
                                                                                      r.XmlDocumentation)).ToArray();

                    if (content.Any())
                    {
                        var html = XmlDocumentation.DocPreview.HtmlDecorateMembers(content);

                        if (language == "C/C++")
                            html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>C++</th>");
                        else if (language == "Basic")
                            html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>VB.NET</th>");
                        else if (language == "F#")
                            html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>F#</th>");

                        var file = GetHtmlFileNameFor(fileName);
                        File.WriteAllText(file, html);
                        return file;
                    }
                }
            }
            catch (Exception e)
            {
                PreviewWindowCommand.Instance.ShowMessageBox(e.Message, "DocPreview");
            }

            return null;
        }

        bool initialized = false;

        string GetHtmlFileNameFor(string sourceFile)
        {
            var key = Path.GetFullPath(sourceFile).ToLower().GetHashCode();
            var rootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocPreview", "Temp");
            var dir = Path.Combine(rootDir, "vs." + System.Diagnostics.Process.GetCurrentProcess().Id);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!initialized)
            {
                initialized = true;
                try
                {
                    var runningProcs = Process.GetProcesses().Select(x => x.Id);

                    var oldDirs = Directory.GetDirectories(rootDir, "vs.*")
                                           .Select(x => new
                                           {
                                               ProcId = int.Parse(Path.GetFileName(x).Replace("vs.", "")),
                                               Dir = x
                                           })
                                           //.Where(x => Process.GetProcessById(x.ProcId) == null) //throws exception if proc is not running
                                           .Where(x => !runningProcs.Contains(x.ProcId))
                                           .Select(x => x.Dir);

                    foreach (var d in oldDirs)
                        try
                        {
                            foreach (var file in Directory.GetFiles(d, "*", SearchOption.AllDirectories))
                                File.Delete(file);
                            Directory.Delete(d, true);
                        }
                        catch { }
                }
                catch { }
            }

            return Path.Combine(dir, "doc." + key + ".html");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshPreview(true);
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RefreshPreview(true);
        }

        private void About_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowAbout();
        }

        Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        void ShowReleaseNotes()
        {
            Navigate("http://www.csscript.net/docpreview/release." + this.Version + ".html");
            protectDisplayedProductInfo = true;
        }

        void ShowAbout()
        {
            ShowPreview(DocPreview.Resources.Resource1.about.Replace("{$version}", this.Version.ToString()));
            protectDisplayedProductInfo = true;
        }

        private void OpenForAll_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string htmlFile = GeneratePreviewForAll();
            if (htmlFile != null)
            {
                try
                {
                    System.Diagnostics.Process.Start(htmlFile);
                }
                catch { }
            }
        }

        private void AutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            config.AutoRefresh = AutoRefresh.IsChecked ?? false;
        }

        private void DefaultTheme_Click(object sender, RoutedEventArgs e)
        {
            XmlDocumentation.DocPreview.SetContentTheme(false);
            RefreshPreview(true);
            config.DefaultTheme = true;
            RefreshThemeMenu();
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            XmlDocumentation.DocPreview.SetContentTheme(true);
            RefreshPreview(true);
            config.DefaultTheme = false;
            RefreshThemeMenu();
        }

        void RefreshThemeMenu()
        {
            foreach (MenuItem item in root.ContextMenu.Items)
            {
                item.IsChecked = false;

                if (config.DefaultTheme)
                    item.IsChecked = item.Tag.Equals("default");
                else
                    item.IsChecked = !item.Tag.Equals("default");
            }
        }
    }

    class Config
    {
        public bool AutoRefresh;
        public bool DefaultTheme = true;

        public static Config Load()
        {
            try { return File.ReadAllText(configFile).FromJson<Config>(); }
            catch { return new Config(); }
        }

        public void Save()
        {
            try { File.WriteAllText(configFile, this.ToJson()); } catch { }
        }

        static string configFile = Path.Combine(XmlDocumentation.DocPreview.AppDataDir, "config.json");
    }

    static class Json
    {
        public static string ToJson(this object obj)
        {
            return new JavaScriptSerializer().Serialize(obj);
        }

        public static T FromJson<T>(this string json)
        {
            return new JavaScriptSerializer().Deserialize<T>(json);
        }
    }
}