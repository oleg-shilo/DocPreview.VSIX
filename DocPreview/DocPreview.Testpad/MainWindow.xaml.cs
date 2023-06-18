using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using static DocPreview.PreviewWindowControl;

namespace DocPreview.Testpad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, IIdeServices
    {
        Config config;

        public MainWindow()
        {
            Runtime.Ide = this;
            config = Config.Load();

            InitializeComponent();
            try
            {
                this.Code.Text = File.ReadAllText(config.LastLoadedFile);
                this.Code.SelectionStart = config.LastSelectionStart;
                this.Code.ScrollToLine(config.LastSelectedLine);
                this.Code.Focus();
            }
            catch { }
        }

        public bool IsCurrentViewValid => !string.IsNullOrEmpty(config.LastLoadedFile);

        public int? GetCurrentCaretLine() => this.Code.Text.GetLineFromPosition(this.Code.SelectionStart) - 1;

        public string GetCurrentViewLanguage() => "CSharp";

        public string GetCurrentViewText() => this.Code.Text;

        public string GetCurrentFileName() => config.LastLoadedFile;

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                config.LastSelectionStart = this.Code.SelectionStart;
                config.LastSelectedLine = this.Code.Text.GetLineFromPosition(this.Code.SelectionStart);
                config.Save();
            }
            catch { }
        }

        void TextBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var fileName = ((string[])e.Data.GetData(DataFormats.FileDrop))?.FirstOrDefault();

            if (fileName != null)
                LoadFile(fileName);
        }

        void LoadFile(string fileName)
        {
            this.Code.Text = File.ReadAllText(fileName);
            this.Code.Focus();
            config.LastLoadedFile = fileName;
            config.Save();
        }

        void TextBox_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        void Code_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.PreviewControl.AutoRefreshPreview();
        }

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Tick += (a, b) =>
            {
                this.PreviewControl.RefreshPreview(true);
                dispatcherTimer.Stop();
            };
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Start();

            ;
        }

        public string[] GetCodeBaseFiles()
            => GetCurrentFileName().ToSingleItemArray();

        void MenuItem_OpenClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.CheckPathExists = true;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
                LoadFile(openFileDialog.FileName);
        }

        void MenuItem_SaveClick(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(config.LastLoadedFile, this.Code.Text);
        }

        void MenuItem_ReloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFile(config.LastLoadedFile);
            }
            catch { }
        }
    }

    class Config
    {
        static string configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        public string LastLoadedFile { get; set; }
        public int LastSelectionStart { get; set; }
        public int LastSelectedLine { get; set; }

        public static Config Load()
        {
            try { return File.ReadAllText(configFile).FromJson<Config>(); }
            catch { return new Config(); }
        }

        public void Save()
        {
            try { File.WriteAllText(configFile, this.ToJson()); } catch { }
        }
    }
}