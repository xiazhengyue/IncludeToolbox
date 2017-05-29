using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IncludeToolbox.Formatter
{
    public static class IncludeFormatter
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
        private static void FormatPaths(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Unchanged)
                return;

            foreach (var line in lines)
            {
                string absoluteIncludeDir = line.TryResolveInclude(includeDirectories, out bool resolvedPath);
                if (resolvedPath)
                    line.IncludeContent = FormatPath(absoluteIncludeDir, pathformat, includeDirectories) ?? line.IncludeContent;
            }
        }

        private static void FormatDelimiters(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.DelimiterMode delimiterMode)
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

        private static void FormatSlashes(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.SlashMode slashMode)
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

        private static void SortIncludes(ref List<IncludeLineInfo> lines, FormatterOptionsPage settings, string documentName)
        {
            FormatterOptionsPage.TypeSorting typeSorting = settings.SortByType;
            bool regexIncludeDelimiter = settings.RegexIncludeDelimiter;
            bool blankAfterRegexGroupMatch = settings.BlankAfterRegexGroupMatch;
            bool removeEmptyLines = settings.RemoveEmptyLines;

            string[] precedenceRegexes = RegexUtils.FixupRegexes(settings.PrecedenceRegexes, documentName);

            // Select only valid include lines and sort them. They'll stay in this relative sorted
            // order when rearranged by regex precedence groups.
            var includeLines = lines
                .Where(x => x.LineType != IncludeLineInfo.Type.NoInclude)
                .OrderBy(x => x.IncludeContent);

            // Group the includes by the index of the precedence regex they match, or
            // precedenceRegexes.Length for no match, and sort the groups by index.
            var includeGroups = includeLines
                .GroupBy(x =>
                {
                    var includeContent = regexIncludeDelimiter ? x.GetIncludeContentWithDelimiters() : x.IncludeContent;
                    for (int precedence = 0; precedence < precedenceRegexes.Count(); ++precedence)
                    {
                        if (Regex.Match(includeContent, precedenceRegexes[precedence]).Success)
                            return precedence;
                    }

                    return precedenceRegexes.Length;
                }, x => x)
                .OrderBy(x => x.Key);

            // Optional newlines between regex match groups
            var groupStarts = new HashSet<IncludeLineInfo>();
            if (blankAfterRegexGroupMatch && precedenceRegexes.Length > 0 && includeLines.Count() > 1)
            {
                // Set flag to prepend a newline to each group's first include
                foreach (var grouping in includeGroups)
                    groupStarts.Add(grouping.First());
            }

            // Flatten the groups
            var sortedIncludes = includeGroups.SelectMany(x => x.Select(y => y));

            // Sort by angle or quoted delimiters if either of those options were selected
            if (typeSorting == FormatterOptionsPage.TypeSorting.AngleBracketsFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.AngleBrackets ? 0 : 1);
            else if (typeSorting == FormatterOptionsPage.TypeSorting.QuotedFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.Quotes ? 0 : 1);

            // Finally, update the actual lines
            List<IncludeLineInfo> extendedLineList = new List<IncludeLineInfo>(lines.Count);
            {
                var sortedIncludesArray = sortedIncludes.ToArray();
                int sortedIndex = 0;
                for (int i = 0; i < lines.Count; ++i)
                {
                    if (lines[i].ContainsActiveInclude)
                    {
                        var includeLine = sortedIncludesArray[sortedIndex++];

                        // Handle prepending a newline if requested, as long as:
                        // - It's not the first line, and
                        // - We'll remove empty lines or the previous line isn't already a NoInclude
                        if (groupStarts.Contains(includeLine) && i > 0 && (removeEmptyLines || !extendedLineList[i - 1].ContainsActiveInclude))
                        {
                            extendedLineList.Add(new IncludeLineInfo());
                        }

                        lines[i] = includeLine;
                    }

                    extendedLineList.Add(lines[i]);
                }
            }

            // Only overwrite original array if we've added lines.
            if(lines.Count != extendedLineList.Count)
                lines = extendedLineList;
        }


        /// <summary>
        /// Formats all includes in a given piece of text.
        /// </summary>
        /// <param name="text">Text to be parsed for includes.</param>
        /// <param name="documentName">Path to the document the edit is occuring in.</param>
        /// <param name="includeDirectories">A list of include directories</param>
        /// <param name="settings">Settings that determine how the formating should be done.</param>
        /// <returns>Formated text.</returns>
        public static string FormatIncludes(string text, string documentPath, IEnumerable<string> includeDirectories, FormatterOptionsPage settings)
        {
            string documentDir = Path.GetDirectoryName(documentPath);
            string documentName = Path.GetFileNameWithoutExtension(documentPath);

            includeDirectories = new string[] { Microsoft.VisualStudio.PlatformUI.PathUtil.Normalize(documentDir) + Path.DirectorySeparatorChar }.Concat(includeDirectories);

            string newLineChars = Utils.GetDominantNewLineSeparator(text);

            var lines = IncludeLineInfo.ParseIncludes(text, settings.RemoveEmptyLines ? ParseOptions.RemoveEmptyLines : ParseOptions.None);

            // Format.
            IEnumerable<string> formatingDirs = includeDirectories;
            if (settings.IgnoreFileRelative)
            {
                formatingDirs = formatingDirs.Skip(1);
            }
            FormatPaths(lines, settings.PathFormat, formatingDirs);
            FormatDelimiters(lines, settings.DelimiterFormatting);
            FormatSlashes(lines, settings.SlashFormatting);

            // Sorting. Ignores non-include lines.
            SortIncludes(ref lines, settings, documentName);

            // Combine again.
            return string.Join(newLineChars, lines.Select(x => x.RawLine));
        }
    }
}
