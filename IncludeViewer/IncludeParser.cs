using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IncludeViewer
{
    /// <summary>
    /// Interface to the native include parser.
    /// </summary>
    internal static class IncludeParser
    {
        public static void test()
        {
            byte[] utf8String = Encoding.UTF8.GetBytes("C:/Users/Andreas/Development/current_development\\IncludeViewer/IncludeParser/IncludeParser.cpp");

            StringHandle handle;
            Result r = Test(utf8String, out handle);
            if (r == Result.Success)
            {
                string str = handle.ResolveString();
            }
        }


        private enum Result : Int32
        {
            Failure,
            Success
        };

        struct StringHandle
        {
            private Int32 Handle;

            public string ResolveString()
            {
                Int32 bufferSize;
                if (GetStringLength(this, out bufferSize) != Result.Success)
                    return null;

                byte[] buffer = new byte[bufferSize];
                IncludeParser.ResolveString(this, buffer, bufferSize);

                return Encoding.UTF8.GetString(buffer);
            }
        }

        [DllImport("IncludeParser.dll")]
        public static extern void Init();
        [DllImport("IncludeParser.dll")]
        public static extern void Exit();

        [DllImport("IncludeParser.dll")]
        private static extern unsafe Result ResolveString(StringHandle handle, byte[] buffer, Int32 bufferSize);

        [DllImport("IncludeParser.dll")]
        private static extern Result GetStringLength(StringHandle handle, out Int32 outBufferSize);

        [DllImport("IncludeParser.dll")]
        private static extern Result Test(byte[] absoluteIncludeFilename, out StringHandle outString);
    }
}
