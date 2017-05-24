using IncludeToolbox.Graph;
using System.Collections.Generic;

namespace IncludeToolbox.GraphWindow
{
    public class HierarchyIncludeTreeViewItem : IncludeTreeViewItem
    {
        private Graph.IncludeGraph.GraphItem item;

        public override IReadOnlyList<IncludeTreeViewItem> Children
        {
            get
            {
                if (cachedItems == null)
                    GenerateChildItems();
                return cachedItems;
            }
        }
        protected IReadOnlyList<IncludeTreeViewItem> cachedItems;

        public HierarchyIncludeTreeViewItem(IncludeGraph.GraphItem graphItem)
        {
            Reset(graphItem);
        }

        private void GenerateChildItems()
        {
            if (item?.Includes != null)
            {
                var cachedItemsList = new List<IncludeTreeViewItem>();
                foreach (Graph.IncludeGraph.Include include in item?.Includes)
                {
                    cachedItemsList.Add(new HierarchyIncludeTreeViewItem(include.IncludedFile));
                }
                cachedItems = cachedItemsList;
            }
            else
            {
                cachedItems = emptyList;
            }
        }

        public void Reset(Graph.IncludeGraph.GraphItem graphItem)
        {
            item = graphItem;
            cachedItems = null;
            Name = graphItem?.FormattedName ?? "";
            AbsoluteFilename = graphItem?.AbsoluteFilename;

            NotifyAllPropertiesChanged();
        }
    }
}
