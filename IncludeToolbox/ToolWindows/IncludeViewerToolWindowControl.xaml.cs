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
using IncludeToolbox.IncludeGraph;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeViewer
{
    using IncludeToolbox.IncludeToolbox;
    using System;
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

        private IncludeGraph graph = null;

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

        private void PopulateTreeWidgetRecursive(ItemCollection target, IEnumerable<IncludeGraph.Include> includes, IEnumerable<string> includeDirectories)
        {
            foreach (var elem in includes)
            {
                string fullIncludePath = elem.IncludedFile.AbsoluteFilename;
                string includeName = IncludeFormatter.FormatPath(fullIncludePath, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories) ?? fullIncludePath;

                var newItem = new TreeViewItem()
                {
                    Header = includeName,
                    ToolTip = fullIncludePath,
                    // Todo: Styling should be part of XAML, but there were some exceptions I don't understand yet
                    Foreground = GetSolidBrush(EnvironmentColors.ToolWindowTextBrushKey),
                    // Todo: Unselected looks weird.
                };

                target.Add(newItem);
                
                if (elem.IncludedFile.Includes != null)
                    PopulateTreeWidgetRecursive(newItem.Items, elem.IncludedFile.Includes, includeDirectories);
            }
        }

        private void Click_Refresh(object sender, RoutedEventArgs e)
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            var newGraph = new IncludeGraph();
            if (newGraph.AddIncludesRecursively_ShowIncludesCompilation(currentDocument, OnNewTreeComputed))
            {
                FileNameLabel.Content = currentDocument.Name;
                ProgressBar.Visibility = Visibility.Visible;
                NumIncludes.Content = "";
                IncludeTree.Items.Clear();
                RefreshButton.IsEnabled = false;
            }
        }

        private void PopulateDGMLGraph(DGMLGraph graph, IncludeGraph.GraphItem item, IEnumerable<string> includeDirectories)
        {
            // TODO: Port to IncludeGraph

            string fullIncludePath = item.AbsoluteFilename;
            string includeName = IncludeFormatter.FormatPath(fullIncludePath, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories) ?? fullIncludePath;

            graph.Nodes.Add(new DGMLGraph.Node { Id = fullIncludePath, Label = includeName });
            
            foreach (var link in item.Includes)
            {
                graph.Links.Add(new DGMLGraph.Link { Source = fullIncludePath, Target = link.IncludedFile.AbsoluteFilename });
                PopulateDGMLGraph(graph, link.IncludedFile, includeDirectories);
            }
        }

        private void Click_SaveGraph(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                Output.Instance.ErrorMsg("There is no include tree to save!");
                return;
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ".dgml";
            dlg.DefaultExt = ".dgml";
            dlg.Filter = "Text documents (.dgml)|*.dgml";

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (!result ?? false)
                return;

            var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
            DGMLGraph dgmlGraph = new DGMLGraph();
            PopulateDGMLGraph(dgmlGraph, graph.CreateOrGetItem(currentDocument.FullName), includeDirectories);
            dgmlGraph.Serialize(dlg.FileName);
        }

        private void OnNewTreeComputed(IncludeGraph graph, bool success)
        {
            ProgressBar.Visibility = Visibility.Hidden;
            RefreshButton.IsEnabled = true;

            if (success)
            {
                this.graph = graph;
                FileNameLabel.Content = currentDocument.Name;
                NumIncludes.Content = (graph.NumGraphItems - 1).ToString(); // The document is itself part of the graph.
                ButtonSaveGraph.IsEnabled = true;

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(currentDocument.Path) + Path.DirectorySeparatorChar);
                PopulateTreeWidgetRecursive(IncludeTree.Items, graph.CreateOrGetItem(currentDocument.FullName).Includes, includeDirectories);
            }
            else
            {
                FileNameLabel.Content = "";
                NumIncludes.Content = "";
                ButtonSaveGraph.IsEnabled = false;
            }
        }
    }
}