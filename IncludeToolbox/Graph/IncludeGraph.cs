using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;

namespace IncludeToolbox.Graph
{
    /// <summary>
    /// Graph of files including files.
    /// </summary>
    public class IncludeGraph
    {
        public struct Include
        {
            public Include(GraphItem file)
            {
                IncludedFile = file;
                IncludeLine = null;
            }

            /// <summary>
            /// Absolute path to the file that includes another file.
            /// </summary>
            public GraphItem IncludedFile { get; private set; }

            /// <summary>
            /// The original include line in sourceFile.
            /// </summary>
            /// <remarks>Depending on the graph generation algorithm, this may be null.</remarks>
            Formatter.IncludeLineInfo IncludeLine;
        }

        public class GraphItem
        {
            public GraphItem(string absoluteFilename)
            {
                AbsoluteFilename = absoluteFilename;
                FormattedName = absoluteFilename;
                Includes = new List<Include>();
            }

            /// <summary>
            /// Absolute path to the file that includes the other files.
            /// </summary>
            /// <remarks>
            /// If an absolute filename can't be provided (e.g. due to resolve failure), this can be any kind of unique file identifier.
            /// This is also used as key in the graph item dictionary.
            /// </remarks>
            public string AbsoluteFilename { get; private set; }

            /// <summary>
            /// A formatted name that can be set from the outside. Is by default the same as AbsoluteFilename.
            /// </summary>
            public string FormattedName { get; set; }

            /// <summary>
            /// List of all includes of this file.
            /// </summary>
            public List<Include> Includes { get; private set; }


            // GraphItem is uniquely identified by its filename.
            /*
            public static bool operator ==(GraphItem a, GraphItem b)
            {
                return a.AbsoluteFilename.Equals(b.AbsoluteFilename);
            }

            public static bool operator !=(GraphItem a, GraphItem b)
            {
                return !(a == b);
            }

            public override int GetHashCode()
            {
                return AbsoluteFilename.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is GraphItem))
                    return false;
                return (GraphItem)obj == this;
            }
            */
        }


        public IReadOnlyCollection<GraphItem> GraphItems => graphItems.Values;

        /// <summary>
        /// Map of all files that the graph reaches.
        /// Use CreateOrAddItem to populate it.
        /// </summary>
        private Dictionary<string, GraphItem> graphItems = new Dictionary<string, GraphItem>();

        /// <summary>
        /// Retrieves item from a given identifying absolute filename.
        /// </summary>
        /// <param name="absoluteFilename">
        /// Absolute path to an include. Will be normalized internally.
        /// If an absolute filename can't be provided (e.g. due to resolve failure), this can be any kind of unique file identifier.
        /// Note however, that this is used as unique identifier.
        /// </param>
        public GraphItem CreateOrGetItem(string absoluteFilename)
        {
            absoluteFilename = PathUtil.Normalize(absoluteFilename);

            GraphItem outItem;
            if (graphItems.TryGetValue(absoluteFilename, out outItem))
                return outItem;
            else
            {
                outItem = new GraphItem(absoluteFilename);
                graphItems.Add(absoluteFilename, outItem);
                return outItem;
            }
        }

        public DGMLGraph ToDGMLGraph()
        {
            DGMLGraph dgmlGraph = new DGMLGraph();
            foreach(GraphItem node in graphItems.Values)
            {
                dgmlGraph.Nodes.Add(new DGMLGraph.Node { Id = node.AbsoluteFilename, Label = node.FormattedName });
                foreach (Include include in node.Includes)
                {
                    dgmlGraph.Links.Add(new DGMLGraph.Link { Source = node.AbsoluteFilename, Target = include.IncludedFile.AbsoluteFilename });
                }
            }

            return dgmlGraph;
        }
    }
}
