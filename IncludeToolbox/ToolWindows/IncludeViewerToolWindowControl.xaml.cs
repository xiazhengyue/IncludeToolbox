using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using IncludeToolbox.IncludeToolbox;
using System.IO;
using Graph = IncludeToolbox.IncludeGraph.IncludeGraph;
using GraphItem = IncludeToolbox.IncludeGraph.IncludeGraph.GraphItem;
using IncludeToolbox.IncludeGraph;
using Formatter = IncludeToolbox.IncludeFormatter.IncludeFormatter;

namespace IncludeToolbox.ToolWindows
{
    /// <summary>
    /// Interaction logic for IncludeViewerToolWindowControl.
    /// </summary>
    public partial class IncludeViewerToolWindowControl : UserControl
    {
        private EnvDTE.Document currentDocument = null;
        private Graph graph = null;

        public IncludeTreeViewItem IncludeTreeModel { get; private set; } = new IncludeTreeViewItem(null);

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeViewerToolWindowControl"/> class.
        /// </summary>
        public IncludeViewerToolWindowControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private static Brush GetSolidBrush(ThemeResourceKey themeResourceKey)
        {
            var color = VSColorTheme.GetThemedColor(themeResourceKey);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private void Click_Refresh(object sender, RoutedEventArgs e)
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            var newGraph = new Graph();
            if (newGraph.AddIncludesRecursively_ShowIncludesCompilation(currentDocument, OnNewTreeComputed))
            {
                FileNameLabel.Content = currentDocument.Name;
                ProgressBar.Visibility = Visibility.Visible;
                NumIncludes.Content = "";
                IncludeTreeModel.Reset(null);
                RefreshButton.IsEnabled = false;
            }
        }

        private void PopulateDGMLGraph(DGMLGraph graph, GraphItem item)
        {
            // TODO: Port to IncludeGraph

            string fullIncludePath = item.AbsoluteFilename;
            string includeName = item.FormattedName;

            graph.Nodes.Add(new DGMLGraph.Node { Id = fullIncludePath, Label = includeName });
            
            foreach (var link in item.Includes)
            {
                graph.Links.Add(new DGMLGraph.Link { Source = fullIncludePath, Target = link.IncludedFile.AbsoluteFilename });
                PopulateDGMLGraph(graph, link.IncludedFile);
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

            DGMLGraph dgmlGraph = new DGMLGraph();
            PopulateDGMLGraph(dgmlGraph, graph.CreateOrGetItem(currentDocument.FullName));
            dgmlGraph.Serialize(dlg.FileName);
        }

        private void OnNewTreeComputed(Graph graph, bool success)
        {
            ProgressBar.Visibility = Visibility.Hidden;
            RefreshButton.IsEnabled = true;

            if (success)
            {
                this.graph = graph;
                FileNameLabel.Content = currentDocument.Name;
                NumIncludes.Content = (graph.GraphItems.Count - 1).ToString(); // The document is itself part of the graph.
                ButtonSaveGraph.IsEnabled = true;

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(currentDocument.Path) + Path.DirectorySeparatorChar);

                foreach(var item in graph.GraphItems)
                    item.FormattedName = Formatter.FormatPath(item.AbsoluteFilename, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories);
                IncludeTreeModel.Reset(graph.CreateOrGetItem(currentDocument.FullName));
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