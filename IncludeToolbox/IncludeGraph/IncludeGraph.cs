using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox.IncludeGraph
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
                IncludeLineNumber = -1;
                IncludeLine = null;
            }

            /// <summary>
            /// Absolute path to the file that includes another file.
            /// </summary>
            public GraphItem IncludedFile { get; private set; }

            /// <summary>
            /// Line in sourceFile which includes includeFile.
            /// May be unknown (-1) depending on the way the graph created.
            /// </summary>
            public int IncludeLineNumber;

            /// <summary>
            /// The original include line in sourceFile.
            /// </summary>
            IncludeFormatter.IncludeLineInfo IncludeLine;


            /*
            // Two includes are treated as the same if they include the same file
            // See GraphItem.Includes for more remarks on this.

            public override int GetHashCode()
            {
                return IncludedFile.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Include))
                    return false;
                return ((Include)obj).IncludedFile == IncludedFile;
            }

            */
        }

        public struct GraphItem
        {
            public GraphItem(string absoluteFilename)
            {
                AbsoluteFilename = absoluteFilename;
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

        public int NumGraphItems { get { return graphItems.Count; } }
    }
}
