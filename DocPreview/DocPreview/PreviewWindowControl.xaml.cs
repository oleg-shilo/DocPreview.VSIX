using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using static DocPreview.PreviewWindowControl;

namespace DocPreview
{
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

            if (config.DefaultZoom == 0)
                config.DefaultZoom = 100;

            for (int i = 50; i <= 200; i += 10)
            {
                object itm;
                ZoomLevel.Items.Add(itm = new ComboBoxItem { Content = $"{i}%" });

                if (i == config.DefaultZoom)
                    ZoomLevel.SelectedItem = itm;
            }

            if (config.Theme == Theme.Default)
                DefaultTheme_Click(null, null);
            else if (config.Theme == Theme.Dark)
                DarkTheme_Click(null, null);
            else
                CustomTheme_Click(null, null);

            Browser.Navigating += Browser_Navigating;
            // Browser.Navigated += (s, e) => SetZoomLevel();

            Browser.LoadCompleted += Browser_LoadCompleted;
            Loaded += MainWindow_Loaded;
            Dispatcher.ShutdownStarted += (s, e) => config.Save();
            DocPreviewPackage.OnLineChanged = AutoRefreshPreview;
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "PreviewWindow");
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            nextNavCanceled = false;
            if (XmlDocumentation.DocPreview.IsVersionFirstRun && !XmlDocumentation.DocPreview.IsFirstEverRun)
                ShowReleaseNotes();
            else
            {
                SetZoomLevel();
                Browser.NavigateToString(initialPrompt.Value);
            }
        }

        void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            nextNavCanceled = true;
            SetZoomLevel();
        }

        bool nextNavCanceled = false;

        void Browser_Navigating(object sender, NavigatingCancelEventArgs e)
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

        public void AutoRefreshPreview()
        {
            if (AutoRefresh.IsChecked.Value)
                RefreshPreview(false);
        }

        bool showingCannotNavigateContent = false;
        bool protectDisplayedProductInfo = false;

        Lazy<string> initialPrompt = new Lazy<string>(XmlDocumentation.DocPreview.GenerateDefaultHtml);

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

        public interface IIdeServices
        {
            bool IsCurrentViewValid { get; }

            string GetCurrentViewLanguage();

            string GetCurrentViewText();

            int? GetCurrentCaretLine();

            string GetCurrentFileName();

            string[] GetCodeBaseFiles();
        }

        public void RefreshPreview(bool force = false)
        {
            try
            {
                if (config.Theme == Theme.Custom)
                    XmlDocumentation.DocPreview.SetContentCustomTheme(config.CustomCss);
                else
                    XmlDocumentation.DocPreview.SetContentTheme(dark: config.Theme == Theme.Dark);

                if (force)
                    protectDisplayedProductInfo = false;

                string language = Runtime.Ide.GetCurrentViewLanguage();

                if (Runtime.Ide.IsCurrentViewValid && (language == "CSharp" || language == "F#" || language == "C/C++" || language == "Basic"))
                {
                    string fileName = Runtime.Ide.GetCurrentFileName();
                    int caretLineNumber = Runtime.Ide.GetCurrentCaretLine().Value;
                    string code = Runtime.Ide.GetCurrentViewText();

                    if (force || (fileName != lastPreviewFileName || caretLineNumber != lastPreviewLineNumber || code != lastPreviewAllText))
                    {
                        lastPreviewFileName = fileName;
                        lastPreviewLineNumber = caretLineNumber;
                        lastPreviewAllText = code;

                        var html = "";

                        Parser.Result result = Parser.FindMemberDocumentation(code, caretLineNumber + 1, language);

                        if (result.Success)
                        {
                            html = XmlDocumentation.DocPreview
                                                       .GenerateHtml(result.MemberTitle,
                                                                     result.MemberDefinition,
                                                                     result.XmlDocumentation);
                            if (language == "C/C++")
                                html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>C++</th>");
                            else if (language == "Basic")
                                html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>VB.NET</th>");
                            else if (language == "F#")
                                html = html.Replace("<th class='CodeTable'>C#</th>", "<th class='CodeTable'>F#</th>");
                        }
                        else
                            html = XmlDocumentation.DocPreview.GenerateDefaultHtml();

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

        /// <summary>
        /// dsafas
        /// </summary>
        struct StructBase3
        {
            /// <summary>
            /// Fooes the specified arg1-3.
            /// </summary>
            /// <param name="arg1">The arg1.</param>
            /// <param name="arg2">The arg2.</param>
            /// <param name="arg3">The arg3.</param>
            /// <returns></returns>
            void foo3(int arg1, string arg2, string arg3) { }
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
                    var runningProcs = System.Diagnostics.Process.GetProcesses().Select(x => x.Id);

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

        void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshPreview(true);
        }

        void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RefreshPreview(true);
        }

        void About_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        void OpenForAll_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        void AutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            config.AutoRefresh = AutoRefresh.IsChecked ?? false;
        }

        void DefaultTheme_Click(object sender, RoutedEventArgs e)
        {
            config.Theme = Theme.Default;
            RefreshPreview(true);
            RefreshThemeMenu();
        }

        void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            config.Theme = Theme.Dark;
            RefreshPreview(true);
            RefreshThemeMenu();
        }

        void CustomTheme_Click(object sender, RoutedEventArgs e)
        {
            config.Theme = Theme.Custom;
            RefreshPreview(true);
            RefreshThemeMenu();
        }

        void ShowCustomTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{config.CustomCss}\"");
            }
            catch { }
        }

        void RefreshThemeMenu()
        {
            var checkBoxItems = root.ContextMenu.Items.OfType<MenuItem>().Where(x => x.Tag != null);

            foreach (MenuItem item in checkBoxItems)
            {
                item.IsChecked = item.Tag.Equals(config.Theme.ToString().ToLower());
            }
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            // works but does not match _axIWebBrowser2 zoom
            var zoom = this.Browser.InvokeScript("getzoom");
        }

        void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
           => Dispatcher.BeginInvoke((Action)SetZoomLevel); // important to let COM object to do it asynchronously.

        void SetZoomLevel()
        {
            try
            {
                var zoomLevel = int.Parse(ZoomLevel.Text.Replace("%", ""));

                var wb = (dynamic)this.Browser.GetType()
                                              .GetField("_axIWebBrowser2",
                                                        BindingFlags.Instance | BindingFlags.NonPublic)
                                              .GetValue(this.Browser);

                wb.ExecWB(63, 2, zoomLevel, ref zoomLevel);   // OLECMDID_OPTICAL_ZOOM (63) - don't prompt (2)
                config.DefaultZoom = zoomLevel;
            }
            catch
            {
                // the control may not be ready yet
            }
        }
    }

    public enum Theme
    {
        Default,
        Dark,
        Custom
    }

    class Config
    {
        public bool AutoRefresh { get; set; }
        public int DefaultZoom { get; set; }
        public Theme Theme { get; set; }

        public string CustomCss { get; set; } = XmlDocumentation.DocPreview.CustomCss;

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
}