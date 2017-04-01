using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IncludeToolbox.IncludeFormatter
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
                string absoluteIncludeDir = line.TryResolveInclude(includeDirectories);
                if (!string.IsNullOrEmpty(absoluteIncludeDir))
                    line.IncludeContent = FormatPath(absoluteIncludeDir, pathformat, includeDirectories) ?? line.IncludeContent;
            }
        }

        private static void FormatDelimiters(IncludeLineInfo[] lines, FormatterOptionsPage.DelimiterMode delimiterMode)
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

        private static void FormatSlashes(IncludeLineInfo[] lines, FormatterOptionsPage.SlashMode slashMode)
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

        public const string CurrentFileNameKey = "$(currentFilename)";

        private static string[] FixupRegexes(string[] precedenceRegexes, string documentName)
        {
            string currentFilename = documentName.Substring(0, documentName.LastIndexOf('.'));

            string[] regexes = new string[precedenceRegexes.Length];
            for (int i = 0; i < precedenceRegexes.Length; ++i)
            {
                regexes[i] = precedenceRegexes[i].Replace(CurrentFileNameKey, currentFilename);
            }
            return regexes;
        }

        private static void SortIncludes(IncludeLineInfo[] lines, FormatterOptionsPage settings, string documentName)
        {
            FormatterOptionsPage.TypeSorting typeSorting = settings.SortByType;
            bool regexIncludeDelimiter = settings.RegexIncludeDelimiter;
            bool blankAfterRegexGroupMatch = settings.BlankAfterRegexGroupMatch;
            bool removeEmptyLines = settings.RemoveEmptyLines;

            string[] precedenceRegexes = FixupRegexes(settings.PrecedenceRegexes, documentName);

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
            if (blankAfterRegexGroupMatch && precedenceRegexes.Length > 0 && includeLines.Count() > 1)
            {
                // Set flag to prepend a newline to each group's first include
                foreach (var grouping in includeGroups)
                    grouping.First().PrependNewline = true;
            }

            // Flatten the groups
            var sortedIncludes = includeGroups.SelectMany(x => x.Select(y => y));

            // Sort by angle or quoted delimiters if either of those options were selected
            if (typeSorting == FormatterOptionsPage.TypeSorting.AngleBracketsFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.AngleBrackets ? 0 : 1);
            else if (typeSorting == FormatterOptionsPage.TypeSorting.QuotedFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.Quotes ? 0 : 1);

            // Finally, update the actual lines
            {
                var sortedIncludesArray = sortedIncludes.ToArray();
                int sortedIndex = 0;
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (lines[i].LineType != IncludeLineInfo.Type.NoInclude)
                    {
                        lines[i] = sortedIncludesArray[sortedIndex++];

                        // Handle prepending a newline if requested, as long as:
                        // - It's not the first line, and
                        // - We'll remove empty lines or the previous line isn't already a NoInclude
                        if (lines[i].PrependNewline && i > 0 && (removeEmptyLines || lines[i - 1].LineType != IncludeLineInfo.Type.NoInclude))
                        {
                            lines[i].RawLine = String.Format("{0}{1}", Environment.NewLine, lines[i].RawLine);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Formats all includes in a given piece of text.
        /// </summary>
        /// <param name="text">Text to be parsed for includes.</param>
        /// <param name="documentName">Name of the current document (has influence on sorting.</param>
        /// <param name="includeDirectories">A list of include directories</param>
        /// <param name="settings">Settings that determine how the formating should be done.</param>
        /// <returns>Formated text.</returns>
        public static string FormatIncludes(string text, string documentName, List<string> includeDirectories, FormatterOptionsPage settings)
        {
            var lines = IncludeLineInfo.ParseIncludes(text, settings.RemoveEmptyLines);

            // Format.
            IEnumerable<string> formatingDirs = includeDirectories;
            if (settings.IgnoreFileRelative)
            {
                formatingDirs = formatingDirs.Skip(1);
            }
            FormatPaths(lines, settings.PathFormat, formatingDirs);
            FormatDelimiters(lines, settings.DelimiterFormatting);
            FormatSlashes(lines, settings.SlashFormatting);

            // Apply changes so far.
            foreach (var line in lines)
                line.UpdateRawLineWithIncludeContentChanges();

            // Sorting. Ignores non-include lines.
            SortIncludes(lines, settings, documentName);

            // Combine again.
            return string.Join(Environment.NewLine, lines.Select(x => x.RawLine));
        }
    }
}
