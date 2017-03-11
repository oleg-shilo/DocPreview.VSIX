using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Linq;

namespace DocPreview
{
    internal static class Global
    {
        static public Func<Type, object> GetService = Package.GetGlobalService;

        public static IWpfTextViewHost GetViewHost()
        {
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            GetUserData().GetData(ref guidViewHost, out holder);
            return (IWpfTextViewHost)holder;
        }

        public static IWpfTextView GetTextView()
        {
            return Global.GetViewHost()?.TextView;
        }

        public static IVsUserData GetUserData()
        {
            int mustHaveFocus = 1;//means true
            IVsTextView currentTextView;
            (GetService(typeof(SVsTextManager)) as IVsTextManager).GetActiveView(mustHaveFocus, null, out currentTextView);

            if (currentTextView is IVsUserData)
                return currentTextView as IVsUserData;
            else
                throw new ApplicationException("No text view is currently open");
        }

        public static DTE2 GetDTE2()
        {
            DTE dte = (DTE)GetService(typeof(DTE));
            DTE2 dte2 = dte as DTE2;

            if (dte2 == null)
            {
                return null;
            }

            return dte2;
        }

        ///// <summary>
        ///// Returns an IVsTextView for the given file path, if the given file is open in Visual Studio.
        ///// </summary>
        ///// <param name="filePath">Full Path of the file you are looking for.</param>
        ///// <returns>The IVsTextView for this file, if it is open, null otherwise.</returns>
        //internal static Microsoft.VisualStudio.TextManager.Interop.IVsTextView GetIVsTextView(string filePath)
        //{
        //    var dte2 = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE));
        //    var sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
        //    var serviceProvider = new ServiceProvider(sp);

        //    Microsoft.VisualStudio.Shell.Interop.IVsUIHierarchy uiHierarchy;
        //    uint itemID;
        //    Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame;
        //    IWpfTextView wpfTextView = null;
        //    if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty, out uiHierarchy, out itemID, out windowFrame))
        //    {
        //        // Get the IVsTextView from the windowFrame.
        //        return Microsoft.VisualStudio.Shell.VsShellUtilities.GetTextView(windowFrame);
        //    }

        //    return null;
        //}
    }
}
