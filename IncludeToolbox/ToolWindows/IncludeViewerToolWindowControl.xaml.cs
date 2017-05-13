using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using IncludeToolbox.Graph;
using IncludeToolbox.Formatter;

namespace IncludeToolbox.ToolWindows
{
    /// <summary>
    /// Interaction logic for IncludeViewerToolWindowControl.
    /// </summary>
    public partial class IncludeViewerToolWindowControl : UserControl
    {
        private EnvDTE.Document currentDocument = null;
        private IncludeGraph graph = null;

        public IncludeTreeViewItem IncludeTreeModel { get; private set; } = new IncludeTreeViewItem(null);

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeViewerToolWindowControl"/> class.
        /// </summary>
        public IncludeViewerToolWindowControl()
        {
            InitializeComponent();
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

            var newGraph = new IncludeGraph();
            if (newGraph.AddIncludesRecursively_ShowIncludesCompilation(currentDocument, OnNewTreeComputed))
            {
                FileNameLabel.Content = currentDocument.Name;
                ProgressBar.Visibility = Visibility.Visible;
                NumIncludes.Content = "";
                IncludeTreeModel.Reset(null);
                RefreshButton.IsEnabled = false;
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

            DGMLGraph dgmlGraph = graph.ToDGMLGraph();
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
                NumIncludes.Content = (graph.GraphItems.Count - 1).ToString(); // The document is itself part of the graph.
                ButtonSaveGraph.IsEnabled = true;

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(currentDocument.Path) + Path.DirectorySeparatorChar);

                foreach(var item in graph.GraphItems)
                    item.FormattedName = IncludeFormatter.FormatPath(item.AbsoluteFilename, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories);
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