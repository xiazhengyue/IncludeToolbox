using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeParserQuickTest
{
    internal class Program
    {
        public static void Main()
        {
            IncludeViewer.IncludeParser.Init();

            string[] includeDirs =
            {
                Directory.GetCurrentDirectory() + @"\..\..\..\ezEngine\Code\Engine",
                @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\include",
                @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\atlmfc\include",
                @"C:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\ucrt",
                @"C:\Program Files (x86)\Windows Kits\8.1\Include\um",
                @"C:\Program Files (x86)\Windows Kits\8.1\Include\shared",
                @"C:\Program Files (x86)\Windows Kits\8.1\Include\winrt"
            };
            string[] preprocessorDefines =
            {
                "WIN32",
                "_DEBUG",
                "_WINDOWS",
                "_USRDLL",
                "_WINDLL",
                "_UNICODE",
                "UNICODE",

                //#if defined(_AMD64_) || defined(_X86_)

                "_MSC_VER 1900",

                "_M_X64",
                "_M_AMD64", // _M_IX86
                //"_AMD64_",

                "_MSC_BUILD 0", // ??
                "_MSC_FULL_VER 190023506", // ??
            };

            string includeDirsComposed = includeDirs.Aggregate("", (current, def) => current + (def + ";"));
            string preprocessorDefinitionsComposed = preprocessorDefines.Aggregate("", (current, def) => current + (def + ";"));

            string processedFile;
            var tree = IncludeViewer.IncludeParser.ParseIncludes(
                    Directory.GetCurrentDirectory() + @"\..\..\..\IncludeParser/IncludeParser.cpp",
                                                                     //@"c:\Users\Andreas\Development\current_development\IncludeViewer\ezEngine\Code\Engine\Foundation\Basics.h",
                                                                     //@"C:/Users/Andreas/Development/current_development/IncludeViewer/ezEngine/Code/Engine/Foundation/Basics/Platform/Win/Platform_win.h",
                                                                     //@"C:/Program Files (x86)/Windows Kits/8.1/Include/um/winbase.h",
                                                                     includeDirsComposed, preprocessorDefinitionsComposed, out processedFile);
            IncludeViewer.IncludeParser.Exit();
        }
    }
}
