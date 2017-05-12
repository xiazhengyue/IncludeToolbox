using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IncludeToolbox.Formatter
{
    /// <summary>
    /// A line of text + information about the include directive in this line if any.
    /// Allows for manipulation of the former.
    /// </summary>
    /// <remarks>
    /// This is obviously not a high performance representation of text, but very easy to use for our purposes here.
    /// </remarks>
    public class IncludeLineInfo
    {
        /// <summary>
        /// Parses a given text into IncludeLineInfo objects.
        /// </summary>
        /// <param name="text">A piece of code.</param>
        /// <param name="removeEmptyLines">Whether IncludeLineInfo objects should be created for empty lines.</param>
        /// <param name="ignoreIncludesInPreprocessorConditionals">If true, ignores all includes that are within preprocessor conditionals.</param>
        /// <returns>An array of parsed lines.</returns>
        public static IncludeLineInfo[] ParseIncludes(string text, bool removeEmptyLines, bool ignoreIncludesInPreprocessorConditionals = false)
        {
            var lines = Regex.Split(text, "\r\n|\r|\n");
            if (removeEmptyLines)
            {
                lines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }
            var outInfo = new IncludeLineInfo[lines.Length];

            // Simplistic parsing.
            int openMultiLineComments = 0;
            int openIfdefs = 0;
            for (int line = 0; line < lines.Length; ++line)
            {
                outInfo[line] = new IncludeLineInfo();
                outInfo[line].lineText = lines[line];

                int commentedSectionStart = int.MaxValue;
                int commentedSectionEnd = int.MaxValue;

                // Check for single line comment.
                {
                    int singleLineCommentStart = lines[line].IndexOf("//");
                    if (singleLineCommentStart != -1)
                        commentedSectionStart = singleLineCommentStart;
                }

                // Check for multi line comments.
                {
                    int multiLineCommentStart = lines[line].IndexOf("/*");
                    if (multiLineCommentStart > -1 && multiLineCommentStart < commentedSectionStart)
                    {
                        ++openMultiLineComments;
                        commentedSectionStart = multiLineCommentStart;
                    }
                    
                    int multiLineCommentEnd = lines[line].IndexOf("*/");
                    if (multiLineCommentEnd > -1)
                    {
                        --openMultiLineComments;
                        commentedSectionEnd = multiLineCommentEnd;
                    }
                }

                Func<int, bool> isCommented = pos => (commentedSectionStart == int.MaxValue && openMultiLineComments > 0) || 
                                                     (pos > commentedSectionStart && pos < commentedSectionEnd);

                // Check for #if / #ifdefs.
                if (ignoreIncludesInPreprocessorConditionals)
                {
                    int ifdefStart = lines[line].IndexOf("#if");
                    int ifdefEnd = lines[line].IndexOf("#endif");
                    if (ifdefStart > -1 && !isCommented(ifdefStart))
                    {
                        ++openIfdefs;
                        continue; // There can be only a single preprocessor directive per line.
                    }
                    else if (ifdefEnd > -1 && !isCommented(ifdefEnd))
                    {
                        --openIfdefs;
                        continue; // There can be only a single preprocessor directive per line.
                    }
                }

                int includeOccurence = lines[line].IndexOf("#include");
                if (includeOccurence == -1) // No include found
                    continue;
                if (isCommented(includeOccurence)) // Include commented out
                    continue;
                if (openIfdefs > 0)  // Inside an #ifdef block.
                    continue;

                // Parse include content.
                outInfo[line].delimiter0 = lines[line].IndexOf('\"', includeOccurence + "#include".Length);
                if (outInfo[line].delimiter0 == -1)
                {
                    outInfo[line].delimiter0 = lines[line].IndexOf('<', includeOccurence + "#include".Length);
                    if (outInfo[line].delimiter0 == -1)
                        continue;
                    outInfo[line].delimiter1 = lines[line].IndexOf('>', outInfo[line].delimiter0 + 1);
                }
                else
                {
                    outInfo[line].delimiter1 = lines[line].IndexOf('\"', outInfo[line].delimiter0 + 1);
                }
                if (outInfo[line].delimiter1 == -1)
                    continue;
            }

            return outInfo;
        }


        public enum Type
        {
            Quotes,
            AngleBrackets,
            NoInclude
        }

        public Type LineType
        {
            get
            {
                if (ContainsInclude)
                {
                    if (lineText[delimiter0] == '<')
                        return Type.AngleBrackets;
                    else if (lineText[delimiter0] == '\"')
                        return Type.Quotes;
                }

                return Type.NoInclude;
            }
        }

        public bool ContainsInclude
        {
            get { return delimiter0 != -1; }
        }

        /// <summary>
        /// Changes the type of this line.
        /// </summary>
        /// <param name="newLineType">Type.NoInclude won't have any effect.</param>
        public void SetLineType(Type newLineType)
        {
            if (LineType != newLineType)
            {
                if (newLineType == Type.AngleBrackets)
                {
                    StringBuilder sb = new StringBuilder(lineText);
                    sb[delimiter0] = '<';
                    sb[delimiter1] = '>';
                    lineText = sb.ToString();
                }
                else if (newLineType == Type.Quotes)
                {
                    StringBuilder sb = new StringBuilder(lineText);
                    sb[delimiter0] = '"';
                    sb[delimiter1] = '"';
                    lineText = sb.ToString();
                }
            }
        }


        /// <summary>
        /// Tries to resolve the include (if any) using a list of directories.
        /// </summary>
        /// <param name="includeDirectories">Include directories.</param>
        /// <returns>Empty string if this is not an include, absolute include path if possible or raw include if not.</returns>
        public string TryResolveInclude(IEnumerable<string> includeDirectories)
        {
            if (!ContainsInclude)
                return "";

            string includeContent = IncludeContent;

            foreach (string dir in includeDirectories)
            {
                string candidate = Path.Combine(dir, includeContent);
                if (File.Exists(candidate))
                {
                    return Utils.GetExactPathName(candidate);
                }
            }

            Output.Instance.WriteLine("Unable to resolve include: '{0}'", includeContent);
            return includeContent;
        }

        /// <summary>
        /// Include content with added delimiters.
        /// </summary>
        public string GetIncludeContentWithDelimiters()
        {
            return lineText.Substring(delimiter0, delimiter1 - delimiter0 + 1);
        }


        /// <summary>
        /// Changes in the include content will NOT be reflected immediately in the raw line text. 
        /// </summary>
        /// <see cref="UpdateRawLineWithIncludeContentChanges"/>
        public string IncludeContent
        {
            get
            {
                int length = delimiter1 - delimiter0 - 1;
                return length > 0 ? RawLine.Substring(delimiter0 + 1, length) : "";
            }
            set
            {
                if (!ContainsInclude)
                    return;

                lineText = lineText.Remove(delimiter0 + 1, delimiter1 - delimiter0 - 1);
                lineText = lineText.Insert(delimiter0 + 1, value);
                delimiter1 = delimiter0 + value.Length + 1;
            }
        }

        /// <summary>
        /// Raw line text as found.
        /// </summary>
        public string RawLine
        {
            get { return lineText; }
        }
        private string lineText = "";

        private int delimiter0 = -1;
        private int delimiter1 = -1;
    }
}
