using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IncludeToolbox.Formatter;

namespace Tests
{
    [TestClass]
    public class IncludeFormatingTest
    {
        private static string sourceCode_NoBlanks =
@"#include ""a.h""
#include <b.hpp>
#include ""filename.h""
#include <d_firstanyways>
#include <e_secondanyways>
#include <c.hpp>";

        private static string sourceCode_WithBlanks =
@"#include ""c_third""

#include ""filename.h""

#include <b_second>
// A comment
#include ""z_first""";


        [TestMethod]
        public void Sorting_BlanksAfterRegexGroup()
        {
            // Blanks after groups.
            string expectedFormatedCode_NoBlanks =
@"#include ""filename.h""

#include <d_firstanyways>
#include <e_secondanyways>

#include ""a.h""
#include <b.hpp>
#include <c.hpp>";

            string expectedFormatedCode_WithBlanks =
@"#include ""filename.h""

#include <b_second>

#include ""c_third""
// A comment
#include ""z_first""";


            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.None;
            settings.PrecedenceRegexes = new string[]
                {
                    IncludeToolbox.RegexUtils.CurrentFileNameKey,
                    ".+_.+"
                };
            settings.BlankAfterRegexGroupMatch = true;
            settings.RemoveEmptyLines = false;


            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_NoBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_NoBlanks, formatedCode);
            formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }

        [TestMethod]
        public void Sorting_AngleBracketsFirst()
        {
            // With sort by type.
            string expectedFormatedCode_NoBlanks =
@"#include <d_firstanyways>
#include <e_secondanyways>
#include <b.hpp>
#include <c.hpp>
#include ""filename.h""
#include ""a.h""";


            string expectedFormatedCode_WithBlanks =
@"#include <b_second>

#include ""filename.h""

#include ""c_third""
// A comment
#include ""z_first""";


            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.AngleBracketsFirst;
            settings.PrecedenceRegexes = new string[]
            {
                    IncludeToolbox.RegexUtils.CurrentFileNameKey,
                    ".+_.+"
            };
            settings.BlankAfterRegexGroupMatch = false;
            settings.RemoveEmptyLines = false;

            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_NoBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_NoBlanks, formatedCode);
            formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }

        [TestMethod]
        public void RemoveEmptyLines()
        {
            string expectedFormatedCode_WithBlanks =
@"#include ""filename.h""
#include <b_second>
#include ""c_third""
// A comment
#include ""z_first""";

            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.None;
            settings.PrecedenceRegexes = new string[] { IncludeToolbox.RegexUtils.CurrentFileNameKey };
            settings.BlankAfterRegexGroupMatch = false;
            settings.RemoveEmptyLines = true;

            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }
    }
}
