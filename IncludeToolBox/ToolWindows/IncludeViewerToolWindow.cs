using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Linq;
using IncludeToolbox;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeViewer
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
        private readonly IncludeViewerToolWindowControl graphToolWindowControl;
        //private System.Threading.Tasks.Task parseTask;
        //private long focusToken = 0;

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

        protected override void OnClose()
        {
            base.OnClose();

            // Unregister from window focus changes.
            DTE dte = GetService(typeof(DTE)) as DTE;
            if (dte == null)
            {
                Events events = (Events)dte.Events;
                events.WindowEvents.WindowActivated -= WindowEvents_WindowActivated;
            }
        }

        private void WindowEvents_WindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
        {
            if (gotFocus.Document != null)
            {
                FocusedDocumentChanged(gotFocus.Document);
            }
        }

        string GetPreprocessorDefinitions(VCCLCompilerTool compilerTool)
        {
            // todo
            string builtInDefinitions = @"_MSC_VER 1900;_M_X64;_M_AMD64;_MSC_BUILD 0;_MSC_FULL_VER 190023506;"; //"_AMD64_;"
            
            return builtInDefinitions + compilerTool.PreprocessorDefinitions;
        }

        private void FocusedDocumentChanged(EnvDTE.Document focusedDocument)
        {
            ParseIncludes(focusedDocument);

         //   System.Threading.Interlocked.Increment(ref focusToken);

         //   parseTask = new System.Threading.Tasks.Task(() => ParseIncludes(focusedDocument, focusToken));
         //   parseTask.Start();
        }

        private void ParseIncludes(EnvDTE.Document focusedDocument/*, long callToken*/)
        {
            var project = focusedDocument.ProjectItem.ContainingProject;
            if (project == null)
            {
                Output.Instance.WriteLine("The document {0} is not part of a project.", focusedDocument.Name);
                return;
            }

            var compilerTool = Utils.GetVCppCompilerTool(project);
            if (compilerTool == null)
                return;

            string includeDirs = Utils.GetProjectIncludeDirectories(project).Aggregate("", (current, def) => current + (def + ";")); ;
            string preprocessorDefinitions = GetPreprocessorDefinitions(compilerTool);

            string processedDocument;
            var includeTreeRoot = IncludeParser.ParseIncludes(focusedDocument.FullName, includeDirs, preprocessorDefinitions, out processedDocument);

            // Apply only, if focus token still relevant.
           // if (System.Threading.Interlocked.CompareExchange(ref callToken, -1, focusToken) == -1)
            {
               // graphToolWindowControl.Dispatcher.InvokeAsync(() =>
                {
                    int lineCount = 0;
                    EnvDTE.TextDocument textDocument = focusedDocument.Object() as EnvDTE.TextDocument;
                    if (textDocument != null)
                    {
                        lineCount = textDocument.EndPoint.Line;
                    }
                    int processedLineCount = processedDocument.Count(x => x == '\n');

                    graphToolWindowControl.SetData(focusedDocument.Name, includeTreeRoot, lineCount, processedLineCount);
                }
            }
        }
    }
}
