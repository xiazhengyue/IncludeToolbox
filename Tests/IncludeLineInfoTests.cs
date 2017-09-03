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
   
	#pragma once
 #if
#endif
int main () {}";

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.None);
            Assert.AreEqual(parse.Count, 7);

            Assert.AreEqual("test.h", parse[0].IncludeContent);
            Assert.AreEqual("tüst.hpp", parse[1].IncludeContent);
            Assert.AreEqual("", parse[2].IncludeContent);
            Assert.AreEqual("", parse[3].IncludeContent);
            Assert.AreEqual("", parse[4].IncludeContent);
            Assert.AreEqual("", parse[5].IncludeContent);
            Assert.AreEqual("", parse[6].IncludeContent);

            Assert.AreEqual("#include \"test.h\"", parse[0].RawLine);
            Assert.AreEqual("  #include <tüst.hpp>", parse[1].RawLine);
            Assert.AreEqual("   ", parse[2].RawLine);
            Assert.AreEqual("	#pragma once", parse[3].RawLine);
            Assert.AreEqual(" #if", parse[4].RawLine);
            Assert.AreEqual("#endif", parse[5].RawLine);
            Assert.AreEqual("int main () {}", parse[6].RawLine);

            Assert.AreEqual(IncludeLineInfo.DelimiterType.Quotes, parse[0].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[1].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[2].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[3].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[4].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[5].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[6].LineDelimiterType);

            Assert.AreEqual(true, parse[0].ContainsPreProcessorDirective);
            Assert.AreEqual(true, parse[1].ContainsPreProcessorDirective);
            Assert.AreEqual(false, parse[2].ContainsPreProcessorDirective);
            Assert.AreEqual(true, parse[3].ContainsPreProcessorDirective);
            Assert.AreEqual(true, parse[4].ContainsPreProcessorDirective);
            Assert.AreEqual(true, parse[5].ContainsPreProcessorDirective);
            Assert.AreEqual(false, parse[6].ContainsPreProcessorDirective);


            parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(parse.Count, 6);
            Assert.AreEqual(0, parse[0].LineNumber);
            Assert.AreEqual(1, parse[1].LineNumber);
            Assert.AreEqual(3, parse[2].LineNumber);
            Assert.AreEqual(4, parse[3].LineNumber);
            Assert.AreEqual(5, parse[4].LineNumber);
            Assert.AreEqual(6, parse[5].LineNumber);
        }

        [TestMethod]
        public void SingleLineComments()
        {
            string sourceCode =
@"// #include <not included after all>
#include <include>";

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[0].LineDelimiterType);
            Assert.AreEqual("", parse[0].IncludeContent);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[1].LineDelimiterType);
            Assert.AreEqual("include", parse[1].IncludeContent);
        }

        [TestMethod]
        public void MultiLineComments()
        {
            // Technically some of the code here has C++ compile errors since preprocessor must always start before any whitespace.
            // But we want to handle this gracefully!

            string sourceCode = "/* test // */ #include <there>";
            var parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[0].LineDelimiterType);

            parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            sourceCode = "#include <there> /* test */ ";
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[0].LineDelimiterType);


            sourceCode =
@"#include <there0> /* <commented0>
/* #include <commented1>
sdfsdf // #include <commented2>
dfdf // */ #include <there1>";

            parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[0].LineDelimiterType);
            Assert.AreEqual("there0", parse[0].IncludeContent);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[1].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.None, parse[2].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[3].LineDelimiterType);
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

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines | ParseOptions.IgnoreIncludesInPreprocessorConditionals);
            Assert.AreEqual(2, parse.Count(x => x.LineDelimiterType != IncludeLineInfo.DelimiterType.None));
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[0].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[parse.Count - 1].LineDelimiterType);
            Assert.AreEqual(0, parse[0].LineNumber);
            Assert.AreEqual(1, parse[1].LineNumber);

            parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.KeepOnlyValidIncludes | ParseOptions.IgnoreIncludesInPreprocessorConditionals);
            Assert.AreEqual(2, parse.Count);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[0].LineDelimiterType);
            Assert.AreEqual(IncludeLineInfo.DelimiterType.AngleBrackets, parse[parse.Count - 1].LineDelimiterType);
            Assert.AreEqual(0, parse[0].LineNumber);
            Assert.AreEqual(12, parse[1].LineNumber);

            parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(5, parse.Count(x => x.LineDelimiterType != IncludeLineInfo.DelimiterType.None));
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

            var parse = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);

            bool successfullyResolved = false;

            string resolvedPath = parse[0].TryResolveInclude(includeDirs, out successfullyResolved);
            StringAssert.EndsWith(resolvedPath, "testdata\\subdir\\testinclude.h");
            Assert.AreEqual(true, successfullyResolved);

            resolvedPath = parse[1].TryResolveInclude(includeDirs, out successfullyResolved);
            StringAssert.EndsWith(resolvedPath, "testdata\\testinclude.h");
            Assert.AreEqual(true, successfullyResolved);

            resolvedPath = parse[2].TryResolveInclude(includeDirs, out successfullyResolved);
            Assert.AreEqual("unresolvable", resolvedPath);
            Assert.AreEqual(false, successfullyResolved);

            resolvedPath = parse[3].TryResolveInclude(includeDirs, out successfullyResolved);
            Assert.AreEqual("", resolvedPath);
            Assert.AreEqual(false, successfullyResolved);
        }

        [TestMethod]
        public void MixedLineEndings()
        {
            // The end of this string is tricky as it adds a 3 newlines: \r\n (win), \n (unix), \r (mac old)
            string sourceCode = "#include <a>\n#include <b>\r\n#include <c>\r#include <d>\r\n\n\r";
            var parseWithoutEmpty = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(4, parseWithoutEmpty.Count);
            var parseWithEmpty = IncludeLineInfo.ParseIncludes(sourceCode, ParseOptions.None);
            Assert.AreEqual(6, parseWithEmpty.Count);
        }

        [TestMethod]
        public void PreserveFlag()
        {
            string testCode = @"#include <blorg> // $include-toolbox-preserve$
#include ""test""";

            var parsedLines = IncludeLineInfo.ParseIncludes(testCode, ParseOptions.RemoveEmptyLines);
            Assert.AreEqual(2, parsedLines.Count);
            Assert.IsTrue(parsedLines[0].ShouldBePreserved);
            Assert.IsFalse(parsedLines[1].ShouldBePreserved);
        }
    }
}
