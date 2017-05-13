using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IncludeToolbox.Formatter;

namespace Tests
{
    [TestClass]
    public class IncludeLineInfoTests
    {
        [TestMethod]
        public void SimpleParsing()
        {
            string sourceCode =
@"#include ""test.h""
#include <tüst.hpp>
   
int main () {}";

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, false);
            Assert.AreEqual(parse.Length, 4);

            Assert.AreEqual("test.h", parse[0].IncludeContent);
            Assert.AreEqual("tüst.hpp", parse[1].IncludeContent);
            Assert.AreEqual("", parse[2].IncludeContent);
            Assert.AreEqual("", parse[3].IncludeContent);

            Assert.AreEqual("#include \"test.h\"", parse[0].RawLine);
            Assert.AreEqual("#include <tüst.hpp>", parse[1].RawLine);
            Assert.AreEqual("   ", parse[2].RawLine);
            Assert.AreEqual("int main () {}", parse[3].RawLine);

            Assert.AreEqual(IncludeLineInfo.Type.Quotes, parse[0].LineType);
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[1].LineType);
            Assert.AreEqual(IncludeLineInfo.Type.NoInclude, parse[2].LineType);
            Assert.AreEqual(IncludeLineInfo.Type.NoInclude, parse[3].LineType);


            parse = IncludeLineInfo.ParseIncludes(sourceCode, true);
            Assert.AreEqual(parse.Length, 3);
        }

        [TestMethod]
        public void SingleLineComments()
        {
            string sourceCode =
@"// #include <not included after all>
#include <include>";

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, true);
            Assert.AreEqual(IncludeLineInfo.Type.NoInclude, parse[0].LineType);
            Assert.AreEqual("", parse[0].IncludeContent);
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[1].LineType);
            Assert.AreEqual("include", parse[1].IncludeContent);
        }

        [TestMethod]
        public void MultiLineComments()
        {
            // Technically some of the code here has C++ compile errors since preprocessor must always start before any whitespace.
            // But we want to handle this gracefully!

            string sourceCode = "/* test // */ #include <there>";
            var parse = IncludeLineInfo.ParseIncludes(sourceCode, true);
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[0].LineType);

            parse = IncludeLineInfo.ParseIncludes(sourceCode, true);
            sourceCode = "#include <there> /* test */ ";
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[0].LineType);


            sourceCode =
@"#include <there0> /* <commented0>
/* #include <commented1>
sdfsdf // #include <commented2>
dfdf // */ #include <there1>";

            parse = IncludeLineInfo.ParseIncludes(sourceCode, true);
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[0].LineType);
            Assert.AreEqual("there0", parse[0].IncludeContent);
            Assert.AreEqual(IncludeLineInfo.Type.NoInclude, parse[1].LineType);
            Assert.AreEqual(IncludeLineInfo.Type.NoInclude, parse[2].LineType);
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[3].LineType);
            Assert.AreEqual("there1", parse[3].IncludeContent);
        }

        [TestMethod]
        public void PreprocessorConditionals()
        {
            string sourceCode =
@"/* #if */ #include <there0>
#if SomeCondition
#include <commented0>
//#include <commented1>
#else
#include <commented2>
//#endif
/*
#endif
*/
#include <commented3>
#endif
#include <there1>";

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, true, true);
            Assert.AreEqual(2, parse.Count(x => x.LineType != IncludeLineInfo.Type.NoInclude));
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[0].LineType);
            Assert.AreEqual(IncludeLineInfo.Type.AngleBrackets, parse[parse.Length - 1].LineType);

            parse = IncludeLineInfo.ParseIncludes(sourceCode, true, false);
            Assert.AreEqual(5, parse.Count(x => x.LineType != IncludeLineInfo.Type.NoInclude));
        }

        [TestMethod]
        public void ResolveIncludes()
        {
            string sourceCode =
@"#include ""testinclude.h""
#include <../testinclude.h>
#include <unresolvable>
#adsfsdf not a include";

            string[] includeDirs =
            {
                "C:/hopefullyyoudonthavethisdir/",
                "garbage",
                System.Environment.CurrentDirectory,
                System.IO.Path.Combine(System.Environment.CurrentDirectory, "testdata/subdir"),
                System.IO.Path.Combine(System.Environment.CurrentDirectory, "testdata"),
            };

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, true);

            string resolvedPath = parse[0].TryResolveInclude(includeDirs);
            StringAssert.EndsWith(resolvedPath, "testdata\\subdir\\testinclude.h");

            resolvedPath = parse[1].TryResolveInclude(includeDirs);
            StringAssert.EndsWith(resolvedPath, "testdata\\testinclude.h");

            resolvedPath = parse[2].TryResolveInclude(includeDirs);
            Assert.AreEqual("unresolvable", resolvedPath);

            resolvedPath = parse[3].TryResolveInclude(includeDirs);
            Assert.AreEqual("", resolvedPath);
        }

        [TestMethod]
        public void MixedLineEndings()
        {
            // The end of this string is tricky as it adds a 3 newlines: \r\n (win), \n (unix), \r (mac old)
            string sourceCode = "#include <a>\n#include <b>\r\n#include <c>\r#include <d>\r\n\n\r";
            var parseWithoutEmpty = IncludeLineInfo.ParseIncludes(sourceCode, true);
            Assert.AreEqual(4, parseWithoutEmpty.Length);
            var parseWithEmpty = IncludeLineInfo.ParseIncludes(sourceCode, false);
            Assert.AreEqual(7, parseWithEmpty.Length);
        }
    }
}
