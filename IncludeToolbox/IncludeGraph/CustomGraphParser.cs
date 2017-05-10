using System;

namespace IncludeToolbox.IncludeGraph
{
    public static class CustomGraphParser
    {
        /// <summary>
        /// Parses a given source file using our own simplified include parsing and adds the output to the original graph.
        /// </summary>
        /// <remarks>
        /// If this is the first file, the graph is necessarily a tree after this operation.
        /// 
        /// For simplicity (one could argue also for completeness) we do not run any form of preprocessor.
        /// This also means, that we need to assume that every include is included always only exactly once.
        /// Obviously, this assumption is generally false! However, in practice a include is rarely included several times intentionally.
        /// </remarks>
        public static void AddIncludesRecursively_ManualParsing(this IncludeGraph graph, string absoluteFilepath)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
