using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeToolbox
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

        public static string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else {
                return di.Name.ToUpper();
            }
        }

        public static VCCLCompilerTool GetVCppCompilerTool(EnvDTE.Document document)
        {
            var project = document.ProjectItem.ContainingProject;
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
            {
                Output.Instance.WriteLine("Cannot find VC++ Project for document '{0}'", document.Name);
                return null;
            }
            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            var tools = activeConfiguration.Tools;
            VCCLCompilerTool compilerTool = null;
            foreach (var tool in activeConfiguration.Tools)
            {
                compilerTool = tool as VCCLCompilerTool;
                if (compilerTool != null)
                    break;
            }

            if (compilerTool == null)
            {
                Output.Instance.WriteLine("Couldn't file a VCCLCompilerTool in VC++ Project.");
                return null;
            }

            return compilerTool;
        }
    }
}
