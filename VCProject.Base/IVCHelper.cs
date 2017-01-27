using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCProjectUtils.Base
{
    public interface IVCHelper
    {
        bool IsVCProject(EnvDTE.Project project);

        bool IsCompilableFile(EnvDTE.Document document, out string reasonForFailure);

        void CompileSingleFile(EnvDTE.Document document);

        string GetCompilerSetting_Includes(EnvDTE.Project project, out string reasonForFailure);

        void SetCompilerSetting_ShowIncludes(EnvDTE.Project project, bool show, out string reasonForFailure);

        bool? GetCompilerSetting_ShowIncludes(EnvDTE.Project document, out string reasonForFailure);

        string GetCompilerSetting_PreprocessorDefinitions(EnvDTE.Project project, out string reasonForFailure);
    }
}
