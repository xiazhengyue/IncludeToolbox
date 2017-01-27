//------------------------------------------------------------------------------
// <copyright file="IncludeViewerToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using EnvDTE;
using IncludeToolbox;
using IncludeToolbox.IncludeFormatter;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeViewer
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for IncludeViewerToolWindowControl.
    /// </summary>
    public partial class IncludeViewerToolWindowControl : UserControl
    {
        private EnvDTE.Document currentDocument = null;
        private bool showIncludeSettingBefore = false;

        private class IncludeTreeItem
        {
            public IncludeTreeItem(string filename, string includeName)
            {
                Filename = filename;
                IncludeName = includeName;
                Children = new List<IncludeTreeItem>();
            }

            public string Filename;
            public string IncludeName;
            public List<IncludeTreeItem> Children;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeViewerToolWindowControl"/> class.
        /// </summary>
        public IncludeViewerToolWindowControl()
        {
            this.InitializeComponent();
        }

        private static Brush GetSolidBrush(ThemeResourceKey themeResourceKey)
        {
            var color = VSColorTheme.GetThemedColor(themeResourceKey);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private void AddIncludes(ItemCollection target, IEnumerable<IncludeTreeItem> includes)
        {
            foreach (var elem in includes)
            {
                var newItem = new TreeViewItem()
                {
                    Header = elem.IncludeName,
                    ToolTip = elem.Filename,
                    // Todo: Styling should be part of XAML, but there were some exceptions I don't understand yet
                    Foreground = GetSolidBrush(EnvironmentColors.ToolWindowTextBrushKey),
                    // Todo: Unselected looks weird.
                };

                target.Add(newItem);
                
                if (elem.Children != null)
                    AddIncludes(newItem.Items, elem.Children);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            string reasonForFailure;
            if (VSUtils.VCUtils.IsCompilableFile(currentDocument, out reasonForFailure) == false)
            {
                Output.Instance.ErrorMsg("Can't refresh since current file can't be compiled: {0}.", reasonForFailure);
                return;
            }

            {
                FileNameLabel.Content = currentDocument.Name;
                ProgressBar.Visibility = Visibility.Visible;
                NumIncludes.Content = "";
                IncludeTree.Items.Clear();
            }

            {
                bool? setting = VSUtils.VCUtils.GetCompilerSetting_ShowIncludes(currentDocument.ProjectItem?.ContainingProject, out reasonForFailure);
                if (!setting.HasValue)
                {
                    Output.Instance.ErrorMsg("Can't compile with show includes: {0}.", reasonForFailure);
                    return;
                }
                else
                    showIncludeSettingBefore = setting.Value;

                VSUtils.VCUtils.SetCompilerSetting_ShowIncludes(currentDocument.ProjectItem?.ContainingProject, true, out reasonForFailure);
                if (!string.IsNullOrEmpty(reasonForFailure))
                {
                    Output.Instance.ErrorMsg("Can't compile with show includes: {0}.", reasonForFailure);
                    return;
                }
            }

            // Even with having the config changed and having compile force==true, we still need to make a dummy change in order to enforce recompilation of this file.
            {
                currentDocument.Activate();
                var documentTextView = VSUtils.GetCurrentTextViewHost();
                var textBuffer = documentTextView.TextView.TextBuffer;
                using (var edit = textBuffer.CreateEdit())
                {
                    edit.Insert(0, " ");
                    edit.Apply();
                }
                using (var edit = textBuffer.CreateEdit())
                {
                    edit.Replace(new Microsoft.VisualStudio.Text.Span(0, 1), "");
                    edit.Apply();
                }
            }

            RefreshButton.IsEnabled = false;
            dte.Events.BuildEvents.OnBuildProjConfigDone += OnBuildConfigFinished;

            try
            {
                VSUtils.VCUtils.CompileSingleFile(currentDocument);
            }
            catch (System.Exception)
            {
                dte.Events.BuildEvents.OnBuildProjConfigDone -= OnBuildConfigFinished;
                RefreshButton.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Hidden;
                VSUtils.VCUtils.SetCompilerSetting_ShowIncludes(currentDocument.ProjectItem?.ContainingProject, showIncludeSettingBefore, out reasonForFailure);
            }
        }

        private void OnBuildConfigFinished(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            var dte = VSUtils.GetDTE();
            dte.Events.BuildEvents.OnBuildProjConfigDone -= OnBuildConfigFinished;
            string reasonForFailure;
            VSUtils.VCUtils.SetCompilerSetting_ShowIncludes(currentDocument.ProjectItem?.ContainingProject, showIncludeSettingBefore, out reasonForFailure);

            try
            { 
                string outputText = "";
                {
                    OutputWindowPane buildOutputPane = null;
                    foreach (OutputWindowPane pane in dte.ToolWindows.OutputWindow.OutputWindowPanes)
                    {
                        if (pane.Guid == VSConstants.OutputWindowPaneGuid.BuildOutputPane_string)
                        {
                            buildOutputPane = pane;
                            break;
                        }
                    }
                    if (buildOutputPane == null)
                    {
                        Output.Instance.ErrorMsg("Failed to query for build output pane!");
                        return;
                    }
                    TextSelection sel = buildOutputPane.TextDocument.Selection;

                    sel.StartOfDocument(false);
                    sel.EndOfDocument(true);

                    outputText = sel.Text;
                }

                IncludeTreeItem outTree = new IncludeTreeItem("", "");
                var includeTreeItemStack = new Stack<IncludeTreeItem>();
                includeTreeItemStack.Push(outTree);

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(currentDocument.Path) + Path.DirectorySeparatorChar);

                const string includeNoteString = "Note: including file: ";
                int numIncludes = 0;
                string[] outputLines = outputText.Split('\n');
                foreach (string line in outputLines)
                {
                    int startIndex = line.IndexOf(includeNoteString);
                    if (startIndex < 0)
                        continue;
                    startIndex += includeNoteString.Length;

                    int includeStartIndex = startIndex;
                    while (includeStartIndex < line.Length && line[includeStartIndex] == ' ')
                        ++includeStartIndex;
                    int depth = includeStartIndex - startIndex;

                    if (depth >= includeTreeItemStack.Count)
                    {
                        includeTreeItemStack.Push(includeTreeItemStack.Peek().Children.Last());
                    }
                    while (depth < includeTreeItemStack.Count - 1)
                        includeTreeItemStack.Pop();

                    string fullIncludePath = line.Substring(includeStartIndex);
                    string resolvedInclude = IncludeFormatter.FormatPath(fullIncludePath, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories) ?? fullIncludePath;
                    includeTreeItemStack.Peek().Children.Add(new IncludeTreeItem(fullIncludePath, resolvedInclude));
                    ++numIncludes;
                }

                AddIncludes(IncludeTree.Items, outTree.Children);
                NumIncludes.Content = numIncludes.ToString();
            }
            finally
            {
                RefreshButton.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Hidden;
            }
        }
    }
}