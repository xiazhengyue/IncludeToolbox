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

        private EnvDTE.Document currentProcessedDocument;
        private System.Threading.Tasks.Task parseTask;

        private EnvDTE.Document queuedDocument;
        private System.Threading.Tasks.Task queuedTask;

        private EnvDTE.Document lastFocusedDocument;

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
            if (gotFocus.Document != null && gotFocus.Document != lastFocusedDocument)
            {
                lastFocusedDocument = gotFocus.Document;
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
            lock (this)
            {
                if (currentProcessedDocument == focusedDocument)
                {
                    queuedTask = null;
                    queuedDocument = null;
                }
                else if (parseTask == null || parseTask.IsCompleted)
                {
                    currentProcessedDocument = focusedDocument;
                    parseTask = new System.Threading.Tasks.Task(() => ParseIncludes(focusedDocument));
                    StartTask(parseTask);
                }
                else if(queuedDocument != focusedDocument || queuedTask == null || queuedTask.IsCompleted)
                {
                    queuedDocument = focusedDocument;
                    queuedTask = new System.Threading.Tasks.Task(() => ParseIncludes(focusedDocument));
                }
            }
        }

        private void ParseIncludes(EnvDTE.Document focusedDocument)
        {
            IncludeParser.IncludeTreeItem includeTreeRoot = null;
            string processedDocument = "";

            try
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

                string includeDirs = Utils.GetProjectIncludeDirectories(project)
                    .Aggregate("", (current, def) => current + (def + ";"));

                string preprocessorDefinitions = GetPreprocessorDefinitions(compilerTool);
                includeTreeRoot = IncludeParser.ParseIncludes(focusedDocument.FullName, includeDirs, preprocessorDefinitions, out processedDocument);
            }
            catch (Exception e)
            {
                graphToolWindowControl.Dispatcher.InvokeAsync(() =>
                {
                    Output.Instance.ErrorMsg("Unexpected error: {0}", e.ToString());
                });
            }
            finally
            {
                // Apply only, if there is not another task queued.
                lock (this)
                {
                    if (queuedTask == null)
                    {
                        graphToolWindowControl.Dispatcher.InvokeAsync(
                            () => ApplyParsingResults(focusedDocument, includeTreeRoot, processedDocument));
                    }
                    else
                    {
                        graphToolWindowControl.Dispatcher.InvokeAsync(StartQueuedTask);
                    }
                }
            }
        }

        private void ApplyParsingResults(EnvDTE.Document focusedDocument, IncludeParser.IncludeTreeItem includeTreeRoot, string processedDocument)
        {
            System.Diagnostics.Debug.Assert(graphToolWindowControl.Dispatcher.Thread == System.Threading.Thread.CurrentThread);

            int lineCount = 0;
            EnvDTE.TextDocument textDocument = focusedDocument.Object() as EnvDTE.TextDocument;
            if (textDocument != null)
            {
                lineCount = textDocument.EndPoint.Line;
            }
            int processedLineCount = processedDocument.Count(x => x == '\n');

            graphToolWindowControl.SetData(focusedDocument.Name, includeTreeRoot, lineCount, processedLineCount);

            lock (this)
            {
                currentProcessedDocument = null;
                parseTask = null;

                // Queued task should be executed?
                if (queuedTask != null)
                {
                    StartQueuedTask();
                }
                else
                {
                    graphToolWindowControl.ProgressBar.Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        private void StartQueuedTask()
        {
            parseTask = queuedTask;
            currentProcessedDocument = queuedDocument;

            queuedDocument = null;
            queuedTask = null;

            StartTask(parseTask);
        }

        private void StartTask(System.Threading.Tasks.Task task)
        {
            graphToolWindowControl.ProgressBar.Visibility = System.Windows.Visibility.Visible;
            task.Start();
        }
    }
}
