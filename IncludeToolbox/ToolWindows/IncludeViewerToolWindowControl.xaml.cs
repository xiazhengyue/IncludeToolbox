using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using IncludeToolbox.Graph;
using IncludeToolbox.Formatter;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public enum RefreshMode
        {
            ShowIncludes,
            DirectParsing,
        }

        public RefreshMode ActiveRefreshMode
        {
            get => activeRefreshMode;
            set
            {
                if (activeRefreshMode != value)
                {
                    activeRefreshMode = value;
                    UpdateRefreshButton();
                }
                //OnPropertyChanged(nameof(ActiveRefreshMode));
            }
        }
        RefreshMode activeRefreshMode;

        public IEnumerable<RefreshMode> PossibleRefreshModes => Enum.GetValues(typeof(RefreshMode)).Cast<RefreshMode>();
           

        // Need to keep these guys alive.
        private EnvDTE.WindowEvents windowEvents;
        //private EnvDTE.BuildEvents buildEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeViewerToolWindowControl"/> class.
        /// </summary>
        public IncludeViewerToolWindowControl()
        {
            InitializeComponent();

            // UI update on dte events.
            var dte = VSUtils.GetDTE();
            if (dte != null)
            {
                windowEvents = dte.Events.WindowEvents;
                windowEvents.WindowActivated += (x,y) => UpdateRefreshButton();
                //buildEvents = dte.Events.BuildEvents;
                //buildEvents.OnBuildBegin += (x, y) => UpdateRefreshButton();
                //buildEvents.OnBuildDone += (x, y) => UpdateRefreshButton();
            }
        }

        private static Brush GetSolidBrush(ThemeResourceKey themeResourceKey)
        {
            var color = VSColorTheme.GetThemedColor(themeResourceKey);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private void UpdateRefreshButton()
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            // In any case we need a it to be a document.
            // Limiting to C++ document is a bit harsh though for the general case as we might not have this information depending on the project type.
            // This is why we just check for "having a document" here for now.
            if (currentDocument == null)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.ToolTip = "No open document";
            }
            else
            {
                if (activeRefreshMode == RefreshMode.ShowIncludes)
                {
                    RefreshButton.IsEnabled = CompilationBasedGraphParser.CanPerformShowIncludeCompilation(currentDocument, out string reasonForFailure);
                    RefreshButton.ToolTip = reasonForFailure;
                }
                else
                {
                    RefreshButton.IsEnabled = true;
                    RefreshButton.ToolTip = null;
                }
            }
        }

        private void Click_Refresh(object sender, RoutedEventArgs e)
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            var newGraph = new IncludeGraph();

            switch (activeRefreshMode)
            {
                case RefreshMode.ShowIncludes:
                    if (newGraph.AddIncludesRecursively_ShowIncludesCompilation(currentDocument, OnNewTreeComputed))
                    {
                        TreeComputeStarted();
                    }
                    break;

                case RefreshMode.DirectParsing:
                    TreeComputeStarted();
                    var settings = (ViewerOptionsPage)IncludeToolboxPackage.Instance.GetDialogPage(typeof(ViewerOptionsPage));
                    var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                    System.Threading.Tasks.Task.Run(
                        () =>
                        {
                            newGraph.AddIncludesRecursively_ManualParsing(currentDocument.FullName, includeDirectories, settings.NoParsePaths);
                        }).ContinueWith(
                        (x) =>
                        {
                            Dispatcher.BeginInvoke((Action)(() => OnNewTreeComputed(newGraph, true)));
                        });
                    break;

                default:
                    throw new NotImplementedException();
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

            // TODO: Progressbar and open prompt.
            DGMLGraph dgmlGraph = graph.ToDGMLGraph();
            dgmlGraph.Serialize(dlg.FileName);
        }

        private void TreeComputeStarted()
        {
            FileNameLabel.Content = currentDocument.Name;
            ProgressBar.Visibility = Visibility.Visible;
            NumIncludes.Content = "";
            IncludeTreeModel.Reset(null);
            RefreshButton.IsEnabled = false;
        }

        private void OnNewTreeComputed(IncludeGraph graph, bool success)
        {
            ProgressBar.Visibility = Visibility.Hidden;
            UpdateRefreshButton();

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
                IncludeTreeModel.Reset(graph.CreateOrGetItem(currentDocument.FullName, out _));
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