using IncludeToolbox.Formatter;
using IncludeToolbox.Graph;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace IncludeToolbox.GraphWindow
{
    public class IncludeGraphViewModel : PropertyChangedBase
    {
        public IncludeTreeViewItem IncludeTreeModel { get; private set; } = new IncludeTreeViewItem(null);

        private IncludeGraph graph = null;
        private EnvDTE.Document currentDocument = null;


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
                    OnNotifyPropertyChanged();
                    UpdateRefreshability();
                }
            }
        }
        RefreshMode activeRefreshMode;

        public IEnumerable<RefreshMode> PossibleRefreshModes => Enum.GetValues(typeof(RefreshMode)).Cast<RefreshMode>();

        public bool CanRefresh
        {
            get => canRefresh;
            private set
            {
                canRefresh = value;
                OnNotifyPropertyChanged();
            }
        }
        bool canRefresh;

        public string RefreshTooltip
        {
            get => refreshTooltip;
            set
            {
                refreshTooltip = value;
                OnNotifyPropertyChanged();
            }
        }
        string refreshTooltip;


        // Need to keep these guys alive.
        private EnvDTE.WindowEvents windowEvents;

        public IncludeGraphViewModel()
        {
            // UI update on dte events.
            var dte = VSUtils.GetDTE();
            if (dte != null)
            {
                windowEvents = dte.Events.WindowEvents;
                windowEvents.WindowActivated += (x, y) => UpdateRefreshability();
            }

            UpdateRefreshability();
        }

        private void UpdateRefreshability()
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            // In any case we need a it to be a document.
            // Limiting to C++ document is a bit harsh though for the general case as we might not have this information depending on the project type.
            // This is why we just check for "having a document" here for now.
            if (currentDocument == null)
            {
                CanRefresh = false;
                RefreshTooltip = "No open document";
            }
            else
            {
                if (activeRefreshMode == RefreshMode.ShowIncludes)
                {
                    CanRefresh = CompilationBasedGraphParser.CanPerformShowIncludeCompilation(currentDocument, out string reasonForFailure);
                    RefreshTooltip = reasonForFailure;
                }
                else
                {
                    CanRefresh = true;
                    RefreshTooltip = null;
                }
            }
        }

        public delegate void GraphCreationFinished(bool success, int numIncludes, string filename);

        public void RefreshIncludeGraph(System.Windows.Threading.Dispatcher dispatcher, GraphCreationFinished finishedCallback)
        {
            var dte = VSUtils.GetDTE();
            currentDocument = dte?.ActiveDocument;

            var newGraph = new IncludeGraph();

            CanRefresh = false;
            RefreshTooltip = "Update in Progress";

            switch (activeRefreshMode)
            {
                case RefreshMode.ShowIncludes:
                    if (newGraph.AddIncludesRecursively_ShowIncludesCompilation(currentDocument, (x,y) => OnNewTreeComputed(x,y, finishedCallback)))
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
                            dispatcher.BeginInvoke((Action)(() => OnNewTreeComputed(newGraph, true, finishedCallback)));
                        });
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void SaveGraph()
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

        private void TreeComputeStarted()
        {
            IncludeTreeModel.Reset(null);
        }

        private void OnNewTreeComputed(IncludeGraph graph, bool success, GraphCreationFinished finishedCallback)
        {
            UpdateRefreshability();

            if (success)
            {
                this.graph = graph;

                finishedCallback(true, graph.GraphItems.Count - 1, currentDocument.Name);

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(currentDocument.Path) + Path.DirectorySeparatorChar);

                foreach (var item in graph.GraphItems)
                    item.FormattedName = IncludeFormatter.FormatPath(item.AbsoluteFilename, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories);
                IncludeTreeModel.Reset(graph.CreateOrGetItem(currentDocument.FullName, out _));
            }
            else
            {
                finishedCallback(false, 0, "");
            }
        }
    }
}
