using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeFormatter
{
    public class IncludeLineInfo
    {
        public static IncludeLineInfo[] ParseIncludes(string text, bool removeEmptyLines, List<string> includeDirectories)
        {
            var lines = text.Split(new[] { Environment.NewLine }, removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
            var outInfo = new IncludeLineInfo[lines.Length];

            // Simplistic parsing.
            // "//" comments are intentionally ignored
            // Todo: Handle multi line comments gracefully
            for (int line = 0; line < lines.Length; ++line)
            {
                outInfo[line] = new IncludeLineInfo();
                outInfo[line].text = lines[line];
                outInfo[line].lineType = IncludeLineInfo.Type.NoInclude;

                int occurence = lines[line].IndexOf("#include");
                if (occurence == -1)
                    continue;

                outInfo[line].Delimiter0 = lines[line].IndexOf('\"', occurence + "#include".Length);
                if (outInfo[line].Delimiter0 == -1)
                {
                    outInfo[line].Delimiter0 = lines[line].IndexOf('<', occurence + "#include".Length);
                    if (outInfo[line].Delimiter0 == -1)
                        continue;
                    outInfo[line].Delimiter1 = lines[line].IndexOf('>', outInfo[line].Delimiter0 + 1);
                    outInfo[line].lineType = IncludeLineInfo.Type.AngleBrackets;
                }
                else
                {
                    outInfo[line].Delimiter1 = lines[line].IndexOf('\"', outInfo[line].Delimiter0 + 1);
                    outInfo[line].lineType = IncludeLineInfo.Type.Quotes;
                }
                if (outInfo[line].Delimiter1 == -1)
                    continue;

                outInfo[line].includeContent = lines[line].Substring(outInfo[line].Delimiter0 + 1, outInfo[line].Delimiter1 - outInfo[line].Delimiter0 - 1);

                // Try to resolve include path to an existing file.
                foreach (string dir in includeDirectories)
                {
                    string candidate = Path.Combine(dir, outInfo[line].IncludeContent);
                    if (File.Exists(candidate))
                    {
                        outInfo[line].AbsoluteIncludePath = Microsoft.VisualStudio.PlatformUI.PathUtil.NormalizePath(candidate);
                        break;
                    }
                }
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
            get { return lineType; }
        }
        private Type lineType;

        public void SetLineType(Type newLineType)
        {
            if (lineType != newLineType)
            {
                lineType = newLineType;
                if(Delimiter0 >= 0 && Delimiter1 >= 0)
                {
                    if (lineType == Type.AngleBrackets)
                    {
                        StringBuilder sb = new StringBuilder(text);
                        sb[Delimiter0] = '<';
                        sb[Delimiter1] = '>';
                        text = sb.ToString();
                    }
                    else if (lineType == Type.Quotes)
                    {
                        StringBuilder sb = new StringBuilder(text);
                        sb[Delimiter0] = '"';
                        sb[Delimiter1] = '"';
                        text = sb.ToString();
                    }
                }
                else
                {
                    lineType = Type.NoInclude;
                }
            }
        }

        public void UpdateTextWithIncludeContent()
        {
            if (lineType == Type.NoInclude)
                return;

            text = text.Remove(Delimiter0 + 1, Delimiter1 - Delimiter0 - 1);
            text = text.Insert(Delimiter0 + 1, includeContent);
            Delimiter1 = Delimiter0 + includeContent.Length + 1;
        }

        public string Text
        {
            get { return text; }
        }
        private string text;

        public string IncludeContent
        {
            get { return includeContent; }
            set { includeContent = value; }
        }
        private string includeContent = "";

        public string AbsoluteIncludePath { get; private set; } = null;

        public int Delimiter0 { get; private set; } = -1;
        public int Delimiter1 { get; private set; } = -1;
    }
}
