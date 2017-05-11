using System.Collections.Generic;
using System.ComponentModel;
using IncludeGraphItem = IncludeToolbox.IncludeGraph.IncludeGraph.GraphItem;
using IncludeGraphInclude = IncludeToolbox.IncludeGraph.IncludeGraph.Include;

namespace IncludeToolbox.ToolWindows
{
    public class IncludeTreeViewItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; private set; }

        public string ToolTip { get; private set; }

        public List<IncludeTreeViewItem> Children
        {
            get
            {
                if (cachedItems == null)
                    GenerateChildItems();
                return cachedItems;
            }
        }
        private List<IncludeTreeViewItem> cachedItems;
        private IncludeGraphItem item;

        public IncludeTreeViewItem(IncludeGraphItem graphItem)
        {
            Reset(graphItem);
        }

        public void Reset(IncludeGraphItem graphItem)
        {
            item = graphItem;
            cachedItems = null;
            Name = graphItem?.FormattedName ?? "";
            ToolTip = graphItem?.AbsoluteFilename ?? "";

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolTip)));
        }

        private void GenerateChildItems()
        {
            cachedItems = new List<IncludeTreeViewItem>();

            if (item?.Includes != null)
            {
                foreach (IncludeGraphInclude include in item?.Includes)
                {
                    cachedItems.Add(new IncludeTreeViewItem(include.IncludedFile));
                }
            }
        }
    }
}
