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
    internal static class VSUtils
    {
        public static EnvDTE80.DTE2 GetDTE()
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if (dte == null)
            {
                throw new System.Exception("Failed to retrieve DTE2!");
            }
            return dte;
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

        public static List<string> GetProjectIncludeDirectories(EnvDTE.Project project, bool endWithSeparator = true)
        {
            List<string> pathStrings = new List<string>();
            if (project == null)
            {
                return pathStrings;
            }

            VCCLCompilerTool compilerTool = GetVCppCompilerTool(project);
            string projectPath = Path.GetDirectoryName(Path.GetFullPath(project.FileName));

            // According to documentation FullIncludePath has resolved macros.
            
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

        public static IWpfTextViewHost GetCurrentTextViewHost()
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
