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
            //    "_USRDLL",
            //    "_WINDLL",
             //   "_UNICODE",
             //   "UNICODE",
            };

            string processedFile;
            var tree = IncludeViewer.IncludeParser.ParseIncludes(
                    //Directory.GetCurrentDirectory() + @"\..\..\..\IncludeParser/IncludeParser.cpp",
                    //@"c:\Users\Andreas\Development\current_development\IncludeViewer\ezEngine\Code\Engine\Foundation\Basics.h",
                    //@"C:/Users/Andreas/Development/current_development/IncludeViewer/ezEngine/Code/Engine/Foundation/Basics/Platform/Win/Platform_win.h",
                    @"D:\Development archive\2016\d3d12gettingstartedplayground\directx12 first steps\D3D12Device.cpp",
                                                                     includeDirs, preprocessorDefines, out processedFile);
            IncludeViewer.IncludeParser.Exit();
        }
    }
}
