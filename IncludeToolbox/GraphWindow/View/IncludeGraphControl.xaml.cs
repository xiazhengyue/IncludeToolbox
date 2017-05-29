using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IncludeToolbox.GraphWindow
{
    public partial class IncludeGraphControl : UserControl
    {
        public IncludeGraphViewModel ViewModel { get; private set; }

        public IncludeGraphControl()
        {
            InitializeComponent();
            ViewModel = (IncludeGraphViewModel)DataContext;
        }

        private void OnIncludeTreeItemMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                if(sender is FrameworkElement frameworkElement)
                {
                    if (frameworkElement.DataContext is IncludeTreeViewItem treeItem)   // Arguably a bit hacky to go over the DataContext, but it seems to be a good direct route.
                    {
                        treeItem.NavigateToInclude();
                    }
                }
            }
        }
    }
}