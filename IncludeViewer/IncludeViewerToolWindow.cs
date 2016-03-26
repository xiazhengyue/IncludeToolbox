using System.Diagnostics;
//using EnvDTE80;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.Shell;

namespace IncludeViewers
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("c87b586a-6c8b-4129-9b6d-56a761e0ac6d")]
    public sealed class IncludeViewerToolWindow : ToolWindowPane
    {
        private IncludeViewerToolWindowControl graphToolWindowControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeViewerToolWindow"/> class.
        /// </summary>
        public IncludeViewerToolWindow() : base()
        {
            this.Caption = "Include Graph";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            graphToolWindowControl = new IncludeViewerToolWindowControl();
            this.Content = graphToolWindowControl;
        }

        public override void OnToolWindowCreated()
        {
            // Register for window focus changes.
            DTE dte = GetService(typeof(DTE)) as DTE;
            if (dte == null)
            {
                Debug.Fail("Can't get EnvDTE80.DTE2 service!");
            }

            Events events = (Events)dte.Events;
            events.WindowEvents.WindowActivated += WindowEvents_WindowActivated;
        }

        private void WindowEvents_WindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
        {
            if (gotFocus.Document != null)
            {
                FocusedDocumentChanged(gotFocus.Document);
            }
        }

        private void FocusedDocumentChanged(EnvDTE.Document focusedDocument)
        {
            VCFileCodeModel fileCodeModel = focusedDocument?.ProjectItem?.FileCodeModel as VCFileCodeModel;
            if (fileCodeModel == null)
            {
                return;
            }
            graphToolWindowControl.SetData(focusedDocument.Name, fileCodeModel);


            VCCodeModel model = (VCCodeModel)focusedDocument.ProjectItem.ContainingProject.CodeModel;
            foreach(var elem in model.Includes)
            {
                Debug.WriteLine(((VCCodeInclude)elem).Name);
            }
            Debug.WriteLine("--");
        }
    }
}
