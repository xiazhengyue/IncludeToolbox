using IncludeToolbox.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox.GraphWindow
{
    public class FolderIncludeTreeViewItem_Root : IncludeTreeViewItem
    {
        IReadOnlyCollection<IncludeGraph.GraphItem> graphItems;
        IncludeGraph.GraphItem includingFile;

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


        public FolderIncludeTreeViewItem_Root(IReadOnlyCollection<IncludeGraph.GraphItem> graphItems, IncludeGraph.GraphItem includingFile)
        {
            this.graphItems = graphItems;
            this.includingFile = includingFile;
            this.Name = "Root";
            this.AbsoluteFilename = "Root";
        }

        public void Reset(IReadOnlyCollection<IncludeGraph.GraphItem> graphItems, IncludeGraph.GraphItem includingFile)
        {
            this.graphItems = graphItems;
            this.includingFile = includingFile;
            this.cachedItems = null;
            NotifyAllPropertiesChanged();
        }

        private void GenerateChildItems()
        {
            if (graphItems == null)
            {
                cachedItems = emptyList;
                return;
            }

            var rootChildren = new List<IncludeTreeViewItem>();
            cachedItems = rootChildren;

            // Build entire tree bottom up. Can't build as lazy as we do with the HierarchyIncludeTreeViewItem.

            // Create all leaf items first.
            var itemsToGroup = new List<IncludeTreeViewItem>();
            foreach (IncludeGraph.GraphItem item in graphItems)
            {
                if (item == includingFile)
                    continue;

                var leaf = new FolderIncludeTreeViewItem_Leaf(item);

                if (string.IsNullOrWhiteSpace(item.AbsoluteFilename) || !Path.IsPathRooted(item.AbsoluteFilename))
                {
                    if (rootChildren.Count == 0)
                        rootChildren.Add(new FolderIncludeTreeViewItem_Folder("<unresolved>"));
                    ((FolderIncludeTreeViewItem_Folder)rootChildren[0]).ChildrenList.Add(leaf);
                }
                else
                    itemsToGroup.Add(leaf);
            }

            // Group leaf items as long as we can
            var groups = new Dictionary<string, FolderIncludeTreeViewItem_Folder>();
            var addedGroups = new List<IncludeTreeViewItem>();
            while (itemsToGroup.Count > 1)
            {
                addedGroups.Clear();

                foreach (var item in itemsToGroup)
                {
                    FolderIncludeTreeViewItem_Folder group = null;
                    string pathUp = Path.GetDirectoryName(item.AbsoluteFilename);
                    if (!string.IsNullOrWhiteSpace(pathUp))
                    {
                        pathUp = pathUp.TrimEnd(Path.DirectorySeparatorChar);
                        if (!groups.TryGetValue(pathUp, out group))
                        {
                            group = new FolderIncludeTreeViewItem_Folder(pathUp);
                            groups.Add(pathUp, group);
                            addedGroups.Add(group);
                        }
                        group.ChildrenList.Add(item);
                    }
                    else
                    {
                        rootChildren.Add(item);
                    }
                }

                itemsToGroup.Clear();
                itemsToGroup.AddRange(addedGroups);
            }

            // Remove unnecessary groups.
            // todo

            if (itemsToGroup.Count == 1)
                rootChildren.AddRange(itemsToGroup[0].Children);
            else
                rootChildren.AddRange(itemsToGroup);
        }
    }

    public class FolderIncludeTreeViewItem_Folder : IncludeTreeViewItem
    {
        public override IReadOnlyList<IncludeTreeViewItem> Children => ChildrenList;
        public List<IncludeTreeViewItem> ChildrenList { get; private set; } = new List<IncludeTreeViewItem>();

        public FolderIncludeTreeViewItem_Folder(string folderFilename)
        {
            Name = folderFilename;
            AbsoluteFilename = Name;
        }
    }

    public class FolderIncludeTreeViewItem_Leaf : IncludeTreeViewItem
    {
        public override IReadOnlyList<IncludeTreeViewItem> Children => emptyList;

        public FolderIncludeTreeViewItem_Leaf(IncludeGraph.GraphItem item)
        {
            Name = item?.FormattedName ?? "";
            AbsoluteFilename = item?.AbsoluteFilename;
        }
    }
}
