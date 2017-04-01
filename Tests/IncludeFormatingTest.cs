using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IncludeToolbox.IncludeFormatter;

namespace Tests
{
    [TestClass]
    public class IncludeFormatingTest
    {
        public void Path()
        {
            // todo
        }

        public void Formatting()
        {
            // todo
        }

        [TestMethod]
        public void Sorting()
        {
            string sourceCode =
@"#include ""a.h""
#include <b.hpp>
#include ""filename.h""
#include <d_firstanyways>
#include <e_secondanyways>
#include <c.hpp>";

            string expectedFormatedCode0 =
@"#include ""filename.h""

#include <d_firstanyways>
#include <e_secondanyways>

#include ""a.h""
#include <b.hpp>
#include <c.hpp>";


            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.None;
            settings.PrecedenceRegexes = new string[]
                {
                    IncludeFormatter.CurrentFileNameKey,
                    ".+_.+"
                };
            settings.BlankAfterRegexGroupMatch = true;

            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode0, formatedCode);


            string expectedFormatedCode1 =
@"#include <d_firstanyways>
#include <e_secondanyways>
#include <b.hpp>
#include <c.hpp>
#include ""filename.h""
#include ""a.h""";

            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.AngleBracketsFirst;
            settings.BlankAfterRegexGroupMatch = false;

            formatedCode = IncludeFormatter.FormatIncludes(sourceCode, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode1, formatedCode);
        }
    }
}
