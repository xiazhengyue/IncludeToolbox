using System.Collections.Generic;
using System.ComponentModel;

namespace IncludeToolbox.GraphWindow
{
    public class IncludeTreeViewItem : PropertyChangedBase
    {
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
        private Graph.IncludeGraph.GraphItem item;

        public IncludeTreeViewItem(Graph.IncludeGraph.GraphItem graphItem)
        {
            Reset(graphItem);
        }

        public void Reset(Graph.IncludeGraph.GraphItem graphItem)
        {
            item = graphItem;
            cachedItems = null;
            Name = graphItem?.FormattedName ?? "";
            ToolTip = graphItem?.AbsoluteFilename ?? "";

            OnNotifyPropertyChanged(nameof(Name));
            OnNotifyPropertyChanged(nameof(Children));
            OnNotifyPropertyChanged(nameof(ToolTip));
        }

        private void GenerateChildItems()
        {
            cachedItems = new List<IncludeTreeViewItem>();

            if (item?.Includes != null)
            {
                foreach (Graph.IncludeGraph.Include include in item?.Includes)
                {
                    cachedItems.Add(new IncludeTreeViewItem(include.IncludedFile));
                }
            }
        }
    }
}
