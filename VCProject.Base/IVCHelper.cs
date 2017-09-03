using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCProjectUtils.Base
{
    // https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.vcprojectengine.machinetypeoption.aspx
    public enum TargetMachineType
    {
        NotSet = 0,
        X86 = 1,
        AM33 = 2,
        ARM = 3,
        EBC = 4,
        IA64 = 5,
        M32R = 6,
        MIPS = 7,
        MIPS16 = 8,
        MIPSFPU = 9,
        MIPSFPU16 = 10,
        MIPSR41XX = 11,
        SH3 = 12,
        SH3DSP = 13,
        SH4 = 14,
        SH5 = 15,
        THUMB = 16,
        AMD64 = 17
    }

    public interface IVCHelper
    {
        bool IsVCProject(EnvDTE.Project project);

        bool IsCompilableFile(EnvDTE.Document document, out string reasonForFailure);

        void CompileSingleFile(EnvDTE.Document document);

        string GetCompilerSetting_Includes(EnvDTE.Project project, out string reasonForFailure);

        void SetCompilerSetting_ShowIncludes(EnvDTE.Project project, bool show, out string reasonForFailure);

        bool? GetCompilerSetting_ShowIncludes(EnvDTE.Project document, out string reasonForFailure);

        string GetCompilerSetting_PreprocessorDefinitions(EnvDTE.Project project, out string reasonForFailure);

        TargetMachineType? GetLinkerSetting_TargetMachine(EnvDTE.Project project, out string reasonForFailure);
    }
}
