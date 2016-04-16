using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeToolbox
{
    internal static class Utils
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
            else
            {
                return di.Name.ToUpper();
            }
        }



        public static VCCLCompilerTool GetVCppCompilerTool(EnvDTE.Project project)
        {
            VCProject vcProject = project?.Object as VCProject;
            if (vcProject == null)
            {
                Output.Instance.WriteLine("Given Project is not a VCProject.");
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

        public static EnvDTE.Document GetActiveDocument()
        {
            EnvDTE.DTE dte = (EnvDTE.DTE) Package.GetGlobalService(typeof (EnvDTE.DTE));
            if (dte == null)
                return null;

            return dte.ActiveDocument;
        }

        public static List<string> GetProjectIncludeDirectories(EnvDTE.Project project, bool endWithSeparator = true)
        {
            VCCLCompilerTool compilerTool = GetVCppCompilerTool(project);
            string projectPath = Path.GetDirectoryName(Path.GetFullPath(project.FileName));

            // According to documentation FullIncludePath has resolved macros.
            List<string> pathStrings = new List<string>();
            pathStrings.AddRange(compilerTool.FullIncludePath.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));

            for (int i = pathStrings.Count - 1; i >= 0; --i)
            {
                try
                {
                    if (!Path.IsPathRooted(pathStrings[i]))
                    {
                        pathStrings[i] = Path.Combine(projectPath, pathStrings[i]);
                    }
                    pathStrings[i] = Utils.GetExactPathName(Path.GetFullPath(pathStrings[i]));

                    if (endWithSeparator)
                        pathStrings[i] += Path.DirectorySeparatorChar;
                }
                catch
                {
                    pathStrings.RemoveAt(i);
                }
            }
            return pathStrings;
        }

        public static IWpfTextViewHost GetCurrentViewHost()
        {
            IVsTextManager textManager = Package.GetGlobalService(typeof (SVsTextManager)) as IVsTextManager;

            IVsTextView textView = null;
            textManager.GetActiveView(1, null, out textView);

            var userData = textView as IVsUserData;
            if (userData == null)
            {
                return null;
            }
            else
            {
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                object holder;
                userData.GetData(ref guidViewHost, out holder);
                var viewHost = (IWpfTextViewHost) holder;

                return viewHost;
            }
        }
    }
}
