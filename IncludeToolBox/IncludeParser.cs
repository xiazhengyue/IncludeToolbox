using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    /// <summary>
    /// Interface to the native include parser.
    /// </summary>
    public static class IncludeParser
    {
        public class IncludeTreeItem
        {
            public IncludeTreeItem(string filename)
            {
                Filename = filename;
                Children = new List<IncludeTreeItem>();
            }

            public string Filename;
            public List<IncludeTreeItem> Children;
        }

        [DllImport("IncludeParser.dll")]
        public static extern void Init();
        [DllImport("IncludeParser.dll")]
        public static extern void Exit();

        public static IncludeTreeItem ParseIncludes(string inputFilename, string includeDirectories, string preprocessorDefinitions, out string processedInputFile)
        {
            IncludeTreeItem outTree = new IncludeTreeItem(inputFilename);

            StringHandle processedInputFileHandle, includeTreeHandle;
            {
                byte[] inputFilenameUtf8 = Encoding.UTF8.GetBytes(inputFilename);
                byte[] includeDirectoriesUtf8 = Encoding.UTF8.GetBytes(includeDirectories);
                byte[] preprocessorDefinitionsUtf8 = Encoding.UTF8.GetBytes(preprocessorDefinitions);

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
            string[] includeTreeRawStrings = includeTreeRaw.Split(new char[]{ '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var includeTreeItemStack = new Stack<IncludeTreeItem>();
            includeTreeItemStack.Push(outTree);
            foreach (string line in includeTreeRawStrings)
            {
                int depth = line.Count(x => x =='\t');
                if (depth >= includeTreeItemStack.Count)
                {
                    includeTreeItemStack.Push(includeTreeItemStack.Peek().Children.Last());
                }
                while (depth < includeTreeItemStack.Count - 1)
                    includeTreeItemStack.Pop();

                includeTreeItemStack.Peek().Children.Add(new IncludeTreeItem(line.Remove(0, depth)));
            }

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

                return Encoding.UTF8.GetString(buffer, 0, buffer.Length - 1); // Exclude \0 at the end.
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
