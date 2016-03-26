using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeFormatter
{
    static class Utils
    {
        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string MakeRelative(string absoluteRoot, string absoluteTarget)
        {
            Uri rootUri = new Uri(absoluteRoot);
            Uri targetUri = new Uri(absoluteTarget);
            if (rootUri.Scheme != targetUri.Scheme)
                return "";

            Uri relativeUri = rootUri.MakeRelativeUri(targetUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }
    }
}
