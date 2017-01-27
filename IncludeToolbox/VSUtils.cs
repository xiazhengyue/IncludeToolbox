using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using VCProjectUtils.Base;

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

        public static IVCHelper VCUtils
        {
            get
            {
                if (vcUtils != null)
                    return vcUtils;
                else
                    return InitVCHelper();
            }
        }

        private static IVCHelper vcUtils;

        private static IVCHelper InitVCHelper()
        {
            var dte = GetDTE();
            if (dte.Version.StartsWith("14."))
                vcUtils = new VCProjectUtils.VS14.VCHelper();
            else if (dte.Version.StartsWith("15."))
                vcUtils = new VCProjectUtils.VS15.VCHelper();

            return vcUtils;
        }

        /// <summary>
        /// Tries to retrieve include directories from a project.
        /// For each encountered path it will try to resolve the paths to absolute paths.
        /// </summary>
        /// <returns>Empty list if include directory retrieval failed.</returns>
        public static List<string> GetProjectIncludeDirectories(EnvDTE.Project project, bool endWithSeparator = true)
        {
            List<string> pathStrings = new List<string>();
            if (project == null)
                return pathStrings;

            string reasonForFailure;
            string projectIncludeDirectories = VCUtils.GetCompilerSetting_Includes(project, out reasonForFailure);
            if(projectIncludeDirectories == null)
            {
                Output.Instance.WriteLine(reasonForFailure);
                return pathStrings;
            }

            string projectPath = Path.GetDirectoryName(Path.GetFullPath(project.FileName));

            // According to documentation FullIncludePath has resolved macros.
            
            pathStrings.AddRange(projectIncludeDirectories.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));

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
