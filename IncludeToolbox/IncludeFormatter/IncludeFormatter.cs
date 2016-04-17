using System;
using System.Collections.Generic;
using System.Linq;

namespace IncludeToolbox.IncludeFormatter
{
    static class IncludeFormatter
    {
        /// <summary>
        /// Formats the paths of a given list of include line info.
        /// </summary>
        /// <param name="pathformat"></param>
        /// <param name="ignoreFileRelative"></param>
        /// <param name="lines"></param>
        /// <param name="includeDirectories"></param>
        public static void FormatPaths(IncludeLineInfo[] lines, FormatterOptionsPage.PathMode pathformat, bool ignoreFileRelative, List<string> includeDirectories)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Unchanged)
                return;

            if (pathformat == FormatterOptionsPage.PathMode.Absolute)
            {
                foreach (var line in lines)
                {
                    // todo: Treat std library files special?
                    if (line.AbsoluteIncludePath != null)
                    {
                        line.IncludeContent = line.AbsoluteIncludePath;
                    }
                }
            }
            else
            {
                foreach (var line in lines)
                {
                    // todo: Treat std library files special?
                    if (line.AbsoluteIncludePath != null)
                    {
                        int bestLength = Int32.MaxValue;
                        string bestCandidate = null;

                        int i = ignoreFileRelative ? 1 : 0; // Ignore first one which is always the local dir.
                        for (; i < includeDirectories.Count; ++i)
                        {
                            string proposal = Utils.MakeRelative(includeDirectories[i], line.AbsoluteIncludePath);

                            if (proposal.Length < bestLength)
                            {
                                if (pathformat == FormatterOptionsPage.PathMode.Shortest ||
                                    (proposal.IndexOf("../") < 0 && proposal.IndexOf("..\\") < 0))
                                {
                                    bestCandidate = proposal;
                                    bestLength = proposal.Length;
                                }
                            }
                        }

                        if (bestCandidate != null)
                        {
                            line.IncludeContent = bestCandidate;
                        }
                    }
                }
            }
        }

        public static void FormatDelimiters(IncludeLineInfo[] lines, FormatterOptionsPage.DelimiterMode delimiterMode)
        {
            switch (delimiterMode)
            {
                case FormatterOptionsPage.DelimiterMode.AngleBrackets:
                    foreach (var line in lines)
                        line.SetLineType(IncludeLineInfo.Type.AngleBrackets);
                    break;
                case FormatterOptionsPage.DelimiterMode.Quotes:
                    foreach (var line in lines)
                        line.SetLineType(IncludeLineInfo.Type.Quotes);
                    break;
            }
        }

        public static void FormatSlashes(IncludeLineInfo[] lines, FormatterOptionsPage.SlashMode slashMode)
        {
            switch (slashMode)
            {
                case FormatterOptionsPage.SlashMode.ForwardSlash:
                    foreach (var line in lines)
                        line.IncludeContent = line.IncludeContent.Replace('\\', '/');
                    break;
                case FormatterOptionsPage.SlashMode.BackSlash:
                    foreach (var line in lines)
                        line.IncludeContent = line.IncludeContent.Replace('/', '\\');
                    break;
            }
        }

        public static void SortIncludes(IncludeLineInfo[] lines, string[] precedenceRegexes, string documentName)
        {
            var comparer = new IncludeComparer(precedenceRegexes, documentName);
            var sortedIncludes = lines.Where(x => x.LineType != IncludeLineInfo.Type.NoInclude).OrderBy(x => x.IncludeContent, comparer).ToArray();
            int incIdx = 0;
            for (int allIdx = 0; allIdx < lines.Length && incIdx < sortedIncludes.Length; ++allIdx)
            {
                if (lines[allIdx].LineType != IncludeLineInfo.Type.NoInclude)
                {
                    lines[allIdx] = sortedIncludes[incIdx];
                    ++incIdx;
                }
            }
        }
    }
}
