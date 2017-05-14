using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;
using System.Windows;

namespace IncludeToolbox.GraphWindow
{
    public partial class IncludeGraphControl : UserControl
    {
        private IncludeGraphViewModel viewModel;

        public IncludeGraphControl()
        {
            InitializeComponent();
            viewModel = (IncludeGraphViewModel)DataContext;
        }

        private void Click_Refresh(object sender, RoutedEventArgs e)
        {
            FileNameLabel.Content = "";
            NumIncludes.Content = "";
            ProgressBar.Visibility = Visibility.Visible;

            viewModel.RefreshIncludeGraph(Dispatcher, OnGraphCreationFinished);
        }

        private void Click_SaveGraph(object sender, RoutedEventArgs e)
        {
            // TODO: Progressbar and open prompt.
            viewModel.SaveGraph();
        }

        private void OnGraphCreationFinished(bool success, int numIncludes, string filename)
        {
            ProgressBar.Visibility = Visibility.Hidden;

            if (success)
            {
                FileNameLabel.Content = filename;
                NumIncludes.Content = numIncludes.ToString();
                ButtonSaveGraph.IsEnabled = false;
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