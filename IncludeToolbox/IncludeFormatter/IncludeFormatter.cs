using System;
using System.Collections.Generic;
using System.Linq;

namespace IncludeToolbox.IncludeFormatter
{
    static class IncludeFormatter
    {
        public static string FormatPath(string absoluteIncludeFilename, FormatterOptionsPage.PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Absolute)
            {
                return absoluteIncludeFilename;
            }
            else
            {
                // todo: Treat std library files special?
                if (absoluteIncludeFilename != null)
                {
                    int bestLength = Int32.MaxValue;
                    string bestCandidate = null;

                    foreach (string includeDirectory in includeDirectories)
                    {
                        string proposal = Utils.MakeRelative(includeDirectory, absoluteIncludeFilename);

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

                    return bestCandidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Formats the paths of a given list of include line info.
        /// </summary>
        public static void FormatPaths(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Unchanged)
                return;

            foreach (var line in lines)
            {
                line.IncludeContent = FormatPath(line.AbsoluteIncludePath, pathformat, includeDirectories) ?? line.IncludeContent;
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

        public static void SortIncludes(IncludeLineInfo[] lines, FormatterOptionsPage.TypeSorting typeSorting, bool regexIncludeDelimiter, string[] precedenceRegexes, string documentName)
        {
            var comparer = new IncludeComparer(precedenceRegexes, documentName);
            var sortedIncludes = lines.Where(x => x.LineType != IncludeLineInfo.Type.NoInclude).OrderBy(x => x.IncludeContentForRegex(regexIncludeDelimiter), comparer);

            if (typeSorting == FormatterOptionsPage.TypeSorting.AngleBracketsFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.AngleBrackets ? 0 : 1);
            else if (typeSorting == FormatterOptionsPage.TypeSorting.QuotedFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.Quotes ? 0 : 1);

            int incIdx = 0;
            var sortedIncludesArray = sortedIncludes.ToArray();
            for (int allIdx = 0; allIdx < lines.Length && incIdx < sortedIncludesArray.Length; ++allIdx)
            {
                if (lines[allIdx].LineType != IncludeLineInfo.Type.NoInclude)
                {
                    lines[allIdx] = sortedIncludesArray[incIdx];
                    ++incIdx;
                }
            }
        }
    }
}
