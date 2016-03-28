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
        public struct IncludeTreeItem
        {
            public string Filename;
            public IncludeTreeItem[] Children;
        }

        [DllImport("IncludeParser.dll")]
        public static extern void Init();
        [DllImport("IncludeParser.dll")]
        public static extern void Exit();

        public static IncludeTreeItem ParseIncludes(string inputFilename, string[] includeDirectories, string[] preprocessorDefinitions, out string processedInputFile)
        {
            IncludeTreeItem outTree = new IncludeTreeItem();
            StringHandle processedInputFileHandle, includeTreeHandle;

            {
                byte[] inputFilenameUtf8 = Encoding.UTF8.GetBytes(inputFilename);
                string includeDirectoriesComposed = includeDirectories.Aggregate("", (current, dir) => current + (dir + ";"));
                byte[] includeDirectoriesUtf8 = Encoding.UTF8.GetBytes(includeDirectoriesComposed);
                string preprocessorDefinitionsComposed = preprocessorDefinitions.Aggregate("", (current, def) => current + (def + ";"));
                byte[] preprocessorDefinitionsUtf8 = Encoding.UTF8.GetBytes(preprocessorDefinitionsComposed);

                Result r = ParseIncludes(inputFilenameUtf8, includeDirectoriesUtf8, preprocessorDefinitionsUtf8,
                                        out processedInputFileHandle, out includeTreeHandle);

                if (r != Result.Success)
                {
                    processedInputFile = "";
                    return outTree;
                }
            }

            processedInputFile = processedInputFileHandle.ResolveString();
            string includeTreeRaw = includeTreeHandle.ResolveString();

            return outTree;
        }

        #region Private

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
                System.Diagnostics.Debug.Assert(Handle >= 0, "Invalid handle, string was already resolved!");

                Int32 bufferSize;
                if (GetStringLength(this, out bufferSize) != Result.Success)
                    return null;

                byte[] buffer = new byte[bufferSize];
                IncludeParser.ResolveString(this, buffer, bufferSize);

                Handle = -1;

                return Encoding.UTF8.GetString(buffer);
            }
        }

        [DllImport("IncludeParser.dll")]
        private static extern unsafe Result ResolveString(StringHandle handle, byte[] buffer, Int32 bufferSize);

        [DllImport("IncludeParser.dll")]
        private static extern Result GetStringLength(StringHandle handle, out Int32 outBufferSize);

        [DllImport("IncludeParser.dll")]
        private static extern Result ParseIncludes(byte[] inputFilename, byte[] includeDirectories, byte[] preprocessorDefinitions, 
                                                    out StringHandle outProcessedInputFile, out StringHandle outIncludeTree);

        #endregion
    }
}
