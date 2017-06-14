using System.Collections.Generic;
using System.Linq;
using static IncludeToolbox.Graph.IncludeGraph;

namespace IncludeToolbox.Graph
{
    /// <summary>
    /// Extension methods to IncludeGraph for DGML interop.
    /// </summary>
    public static class IncludeGraphToDGML
    {
        static public DGMLGraph ToDGMLGraph(this IncludeGraph graph)
        {
            var uniqueTransitiveChildrenMap = FindUniqueChildren(graph.GraphItems);

            DGMLGraph dgmlGraph = new DGMLGraph();
            foreach (GraphItem node in graph.GraphItems)
            {
                dgmlGraph.Nodes.Add(new DGMLGraph.Node
                {
                    Id = node.AbsoluteFilename,
                    Label = node.FormattedName,
                    Background = null,
                    NumIncludes = node.Includes.Count,
                    NumUniqueTransitiveChildren = uniqueTransitiveChildrenMap[node].Count,
                });
                foreach (Include include in node.Includes)
                {
                    dgmlGraph.Links.Add(new DGMLGraph.Link { Source = node.AbsoluteFilename, Target = include.IncludedFile?.AbsoluteFilename ?? null });
                }
            }

            return dgmlGraph;
        }

        /// <summary>
        /// Creates hashlist of all transitive children for all graph items.
        /// </summary>
        static private Dictionary<GraphItem, HashSet<GraphItem>> FindUniqueChildren(IReadOnlyCollection<GraphItem> graphItems)
        {
            var uniqueChildrenLists = new Dictionary<GraphItem, HashSet<GraphItem>>(graphItems.Count);

            // We do not assume that there is a single root node. The graph might contain several independent cpp files.
            foreach (var node in graphItems)
            {
                FindUnqiueChildrenRec(node, uniqueChildrenLists);
                if (uniqueChildrenLists.Count == graphItems.Count)
                    break;
            }

            return uniqueChildrenLists;
        }

        static private IEnumerable<GraphItem> FindUnqiueChildrenRec(GraphItem node, Dictionary<GraphItem, HashSet<GraphItem>> uniqueChildrenMap)
        {
            if (node == null)
                return Enumerable.Empty<GraphItem>();

            HashSet<GraphItem> uniqueChildren;
            if (!uniqueChildrenMap.TryGetValue(node, out uniqueChildren))
            {
                uniqueChildren = new HashSet<GraphItem>();
                uniqueChildrenMap.Add(node, uniqueChildren);    // Add immediately to avoid problems with graph circles.

                foreach (var include in node.Includes)
                {
                    uniqueChildren.Add(include.IncludedFile);
                    uniqueChildren.UnionWith(FindUnqiueChildrenRec(include.IncludedFile, uniqueChildrenMap));
                }
            }
            return uniqueChildren;
        }
    }
}
