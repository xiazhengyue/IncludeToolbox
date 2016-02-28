using System.Diagnostics;
using EnvDTE80;

namespace VSIncludeGraphs
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

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
    public sealed class IncludeGraphToolWindow : ToolWindowPane
    {
        private IncludeGraphToolWindowControl graphToolWindowControl;
        private const string languageFilter = "C/C++";

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeGraphToolWindow"/> class.
        /// </summary>
        public IncludeGraphToolWindow() : base()
        {
            this.Caption = "Include Graph";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            graphToolWindowControl = new IncludeGraphToolWindowControl();
            this.Content = graphToolWindowControl;
        }

        public override void OnToolWindowCreated()
        {
            // Register for window focus changes.
            DTE2 dte = GetService(typeof(DTE2)) as DTE2;
            if (dte == null)
            {
                Debug.Fail("Can't get EnvDTE80.DTE2 service!");
            }

            Events2 events = (Events2)dte.Events;
            events.WindowEvents.WindowActivated += WindowEvents_WindowActivated;

            // Todo: If we want full graphs, we need to update the graph on the following events.
            // Or are we going to use VCCodeModel? https://msdn.microsoft.com/en-us/library/t41260xs.aspx
            //
            //dte.Events.SolutionItemsEvents.ItemRemoved
            //dte.Events.SolutionItemsEvents.ItemAdded
            //dte.Events.SolutionItemsEvents.ItemRenamed
            //dte.Events.SolutionEvents.ProjectRemoved
            //dte.Events.SolutionEvents.ProjectAdded
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
            if (focusedDocument.Language.Equals(languageFilter))
            {
                graphToolWindowControl.SetData(focusedDocument.Name);
            }
        }
    }
}
