using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static DocPreview.PreviewWindowControl;
using Task = System.Threading.Tasks.Task;

namespace DocPreview
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PreviewWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("bd64520c-aa57-4e4d-8793-f8b6e92690ac");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewWindowCommand"/> class. Adds our
        /// command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PreviewWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PreviewWindowCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in PreviewWindowCommand's
            // constructor requires the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PreviewWindowCommand(package, commandService);

            Runtime.Ide = new IdeServices();
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the instance number 0 of this tool window. This window is single instance so this
            // instance is actually the only one. The last flag is set to true so that if the tool
            // window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(PreviewWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowMessageBox(string message, string title)
        {
            VsShellUtilities.ShowMessageBox((IServiceProvider)this.ServiceProvider,
                                            message,
                                            title,
                                            OLEMSGICON.OLEMSGICON_INFO,
                                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    class IdeServices : IIdeServices
    {
        public bool IsCurrentViewValid
        {
            get
            {
                IWpfTextView textView = Global.GetTextView();
                ITextSnapshot snapshot = textView?.TextSnapshot;

                if (snapshot == null || snapshot != snapshot.TextBuffer.CurrentSnapshot)
                    return false;

                if (textView?.Selection.IsEmpty != true)
                    return false;

                return true;
            }
        }

        public int? GetCurrentCaretLine()
        {
            IWpfTextView textView = Global.GetTextView();
            if (textView != null)
            {
                ITextSnapshot snapshot = textView.TextSnapshot;
                return snapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition);
            }
            return null;
        }

        public string GetCurrentViewLanguage()
        {
            IWpfTextView textView = Global.GetTextView();
            return textView.TextBuffer.ContentType.TypeName;
        }

        public string GetCurrentViewText()
        {
            IWpfTextView textView = Global.GetTextView();
            if (textView != null)
            {
                ITextSnapshot snapshot = textView.TextSnapshot;
                int caretLineNumber = snapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition);
                return snapshot.GetText();
            }
            return null;
        }

        public string GetCurrentFileName() => Global.GetDTE2().ActiveDocument.FullName;

        public string[] GetCodeBaseFiles()
        {
            // var projects = Global.GetSolutionProjects();

            // var query = from p in projects
            //             where p.ContainsFile(containedFile)
            //             select p;

            // return query.ToArray();
            return GetCurrentFileName().ToSingleItemArray();
        }
    }
}