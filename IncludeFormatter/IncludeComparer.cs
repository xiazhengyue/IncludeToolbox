using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IncludeFormatter
{
    public class IncludeComparer : IComparer<string>
    {
        public const string CurrentFileNameKey = "$(currentFilename)";

        public IncludeComparer(string[] precedenceRegexes, EnvDTE.Document document)
        {
            string currentFilename = document.Name.Substring(0, document.Name.LastIndexOf('.'));

            this.precedenceRegexes = new string[precedenceRegexes.Length];
            for (int i = 0; i < this.precedenceRegexes.Length; ++i)
            {
                this.precedenceRegexes[i] = precedenceRegexes[i].Replace(CurrentFileNameKey, currentFilename);
            }
        }

        private readonly string[] precedenceRegexes;

        public int Compare(string lineA, string lineB)
        {
            if (lineA == null)
            {
                if (lineB == null)
                    return 0;
                return -1;
            }
            else if (lineB == null)
            {
                return 1;
            }

            int precedenceA = 0;
            for (; precedenceA < precedenceRegexes.Length; ++precedenceA)
            {
                if (Regex.Match(lineA, precedenceRegexes[precedenceA]).Success)
                    break;
            }
            int precedenceB = 0;
            for (; precedenceB < precedenceRegexes.Length; ++precedenceB)
            {
                if (Regex.Match(lineB, precedenceRegexes[precedenceB]).Success)
                    break;
            }

            if (precedenceA == precedenceB)
                return lineA.CompareTo(lineB);
            else
                return precedenceA.CompareTo(precedenceB);
        }
    }
}
