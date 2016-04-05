using System;
using System.Collections.Generic;
using System.Diagnostics;
//using EnvDTE80;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
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

        private VCCLCompilerTool GetCurrentCompilerTool(EnvDTE.Document document)
        {
            var project = document.ProjectItem.ContainingProject;
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
            {
                Output.Error("The given project is not a VC++ Project");
                return null;
            }
            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            var tools = activeConfiguration.Tools;
            VCCLCompilerTool compilerTool = null;
            foreach (var tool in activeConfiguration.Tools)
            {
                compilerTool = tool as VCCLCompilerTool;
                if (compilerTool != null)
                    break;
            }

            if (compilerTool == null)
            {
                Output.Error("Couldn't file a VCCLCompilerTool.");
                return null;
            }

            return compilerTool;
        }

        private string GetIncludeDirectories(VCCLCompilerTool compilerTool, string projectPath)
        {
            // Need to separate to resolve.
            var pathStrings = new List<string>();
            pathStrings.AddRange(compilerTool.FullIncludePath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            for (int i = pathStrings.Count - 1; i >= 0; --i)
            {
                try
                {
                    if (!Path.IsPathRooted(pathStrings[i]))
                    {
                        pathStrings[i] = Path.Combine(projectPath, pathStrings[i]);
                    }
                    pathStrings[i] = Utils.GetExactPathName(Path.GetFullPath(pathStrings[i])) + Path.DirectorySeparatorChar;
                }
                catch
                {
                    pathStrings.RemoveAt(i);
                }
            }
            return pathStrings.Aggregate("", (current, def) => current + (def + ";"));
        }

        string GetPreprocessorDefinitions(VCCLCompilerTool compilerTool)
        {
            // todo
            string builtInDefinitions = @"_MSC_VER 1900;_M_X64;_M_AMD64;_MSC_BUILD 0;_MSC_FULL_VER 190023506;"; //"_AMD64_;"
            
            return builtInDefinitions + compilerTool.PreprocessorDefinitions;
        }


        private void FocusedDocumentChanged(EnvDTE.Document focusedDocument)
        {
            var compilerTool = GetCurrentCompilerTool(focusedDocument);
            if (compilerTool == null)
                return;

            string projectPath = Path.GetDirectoryName(Path.GetFullPath(focusedDocument.ProjectItem.ContainingProject.FileName));
            string includeDirs = GetIncludeDirectories(compilerTool, projectPath);
            string preprocessorDefinitions = GetPreprocessorDefinitions(compilerTool);

            string processedDocument;
            var treeRoot = IncludeParser.ParseIncludes(focusedDocument.FullName, includeDirs, preprocessorDefinitions, out processedDocument);

            int lineCount = 0;
            EnvDTE.TextDocument textDocument = focusedDocument.Object() as EnvDTE.TextDocument;
            if (textDocument != null)
            {
                lineCount = textDocument.EndPoint.Line;
            }
            int processedLineCount = processedDocument.Count(x => x == '\n');
            graphToolWindowControl.SetData(focusedDocument.Name, treeRoot, lineCount, processedLineCount);
        }
    }
}
