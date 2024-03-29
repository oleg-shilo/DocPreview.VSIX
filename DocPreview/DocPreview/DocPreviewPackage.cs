﻿using DocPreview.Resources;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using static DocPreview.PreviewWindowControl;
using Task = System.Threading.Tasks.Task;

namespace DocPreview
{
    public static class Runtime
    {
        static public IIdeServices Ide;

        static public bool initialized = false;

        static public void InitAssemblyProbing(bool isXUnitEnvir = false)
        {
            lock (typeof(Runtime))
            {
                if (!initialized)
                {
                    var localDir = Path.GetDirectoryName(typeof(DocPreviewPackage).Assembly.Location);
                    void deploy_assembly(string name)
                    {
                        var file = Path.Combine(localDir, name + ".dll");
                        if (!File.Exists(file))
                        {
                            object obj = ResourceAssemblies.ResourceManager.GetObject(name.Replace(".", "_"), ResourceAssemblies.Culture);
                            var bytes = ((byte[])(obj));
                            File.WriteAllBytes(file, bytes);
                        }
                    }

                    // deploy_assembly("Microsoft.CodeAnalysis");
                    // deploy_assembly("Microsoft.CodeAnalysis.CSharp");
                    // deploy_assembly("System.Collections.Immutable");

                    if (isXUnitEnvir)
                        deploy_assembly("System.Runtime.CompilerServices.Unsafe");

                    AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
                    {
                        try
                        {
                            var file = Path.Combine(localDir, args.Name.Split(',').FirstOrDefault() + ".dll");
                            if (File.Exists(file))
                                return Assembly.LoadFrom(file);
                        }
                        catch { }
                        return null;
                    };
                    initialized = true;
                }
            }
        }
    }

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio is to
    /// implement the IVsPackage interface and register itself with the shell. This package uses the
    /// helper classes defined inside the Managed Package Framework (MPF) to do it: it derives from
    /// the Package class that provides the implementation of the IVsPackage interface and uses the
    /// registration attributes defined in the framework to register itself and its components with
    /// the shell. These attributes tell the pkgdef creation utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset
    /// Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(DocPreviewPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(PreviewWindow))]
    public sealed class DocPreviewPackage : AsyncPackage
    {
        /// <summary>
        /// DocPreviewPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "c8bd6107-27cf-44a6-82a9-30a7a3d3befa";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token to monitor for initialization cancellation, which can occur when VS
        /// is shutting down.
        /// </param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>
        /// A task representing the async work of package initialization, or an already completed
        /// task if there is none. Do not return null from this method.
        /// </returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at
            // this point. Do any initialization that requires the UI thread after switching to the
            // UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await PreviewWindowCommand.InitializeAsync(this);
        }

        public DocPreviewPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.

            // Runtime.InitAssemblyProbing();

            //var textEditorEvents = Global.GetDTE2().Events.TextEditorEvents;
            //textEditorEvents.LineChanged += TextEditorEvents_LineChanged;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            OnLineChanged?.Invoke();
        }

        //http://stackoverflow.com/questions/16557923/from-a-vs2008-vspackage-how-do-i-get-notified-whenever-caret-position-changed
        //private void TextEditorEvents_LineChanged(EnvDTE.TextPoint StartPoint, EnvDTE.TextPoint EndPoint, int Hint)
        // {
        //}

        static public Action OnLineChanged;

        #endregion Package Members
    }
}