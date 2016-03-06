using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeFormatter
{
    public class IncludeLineInfo
    {
        public static IncludeLineInfo[] ParseIncludes(string text, bool removeEmptyLines)
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
                    outInfo[line].lineType = IncludeLineInfo.Type.IncludeAcute;
                }
                else
                {
                    outInfo[line].Delimiter1 = lines[line].IndexOf('\"', outInfo[line].Delimiter0 + 1);
                    outInfo[line].lineType = IncludeLineInfo.Type.IncludeQuot;
                }
                if (outInfo[line].Delimiter1 == -1)
                    continue;

                outInfo[line].includeContent = lines[line].Substring(outInfo[line].Delimiter0 + 1, outInfo[line].Delimiter1 - outInfo[line].Delimiter0 - 1);
            }

            return outInfo;
        }


        public enum Type
        {
            IncludeQuot,
            IncludeAcute,
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
                    if (lineType == Type.IncludeAcute)
                    {
                        StringBuilder sb = new StringBuilder(text);
                        sb[Delimiter0] = '<';
                        sb[Delimiter1] = '>';
                        text = sb.ToString();
                    }
                    else if (lineType == Type.IncludeQuot)
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

        public string Text
        {
            get { return text; }
        }
        private string text;

        public string IncludeContent
        {
            get { return includeContent; }
        }

        public void ReplaceIncludeContent(string newContent)
        {
            if (lineType == Type.NoInclude)
                return;

            includeContent = newContent;
            text = text.Remove(Delimiter0 + 1, Delimiter1 - Delimiter0 - 1);
            text = text.Insert(Delimiter0 + 1, includeContent);
            Delimiter1 = Delimiter0 + includeContent.Length;
        }
        private string includeContent = "";

        public int Delimiter0 { get; private set; } = -1;
        public int Delimiter1 { get; private set; } = -1;
    }
}
