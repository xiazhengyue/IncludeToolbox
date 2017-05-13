using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IncludeToolbox;
using System.IO;
using IncludeToolbox.Graph;
using System.Collections;

namespace Tests
{
    [TestClass]
    public class IncludeGraphTest
    {
        [TestMethod]
        public void WriteSimpleDGML()
        {
            string filenameTestOutput = "testdata/output.dgml";
            string filenameComparision= "testdata/simplegraph.dgml";
            try
            {
                File.Delete(filenameTestOutput);
            }
            catch { }

            var writer = new DGMLGraph();
            writer.Nodes.Add(new DGMLGraph.Node { Id = "0", Label = "0" });
            writer.Nodes.Add(new DGMLGraph.Node { Id = "1", Label = "1label" });
            writer.Nodes.Add(new DGMLGraph.Node { Id = "2", Label = "2" });
            writer.Nodes.Add(new DGMLGraph.Node { Id = "3", Label = "3" });
            writer.Links.Add(new DGMLGraph.Link { Source = "0", Target = "1" });
            writer.Links.Add(new DGMLGraph.Link { Source = "1", Target = "0", Label = "backlink"});
            writer.Links.Add(new DGMLGraph.Link { Source = "1", Target = "1", Label = "loop" });
            writer.Links.Add(new DGMLGraph.Link { Source = "2", Target = "3", });
            writer.Links.Add(new DGMLGraph.Link { Source = "4", Target = "3", });
            writer.Links.Add(new DGMLGraph.Link { Source = "2", Target = "0", });
            writer.Serialize(filenameTestOutput);

            string expectedFile = File.ReadAllText(filenameComparision);
            string writtenFile = File.ReadAllText(filenameTestOutput);

            Assert.AreEqual(expectedFile, writtenFile);

            // For a clean environment!
            File.Delete(filenameTestOutput);
        }

        [TestMethod]
        public void CustomGraphParse()
        {
            IncludeGraph graph = new IncludeGraph();
            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/source0.cpp"), Enumerable.Empty<string>());
            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/source1.cpp"), Enumerable.Empty<string>());
            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/testinclude.h"), Enumerable.Empty<string>()); // Redundancy shouldn't matter.

            // Check items.
            Assert.AreEqual(5, graph.GraphItems.Count);
            bool newItem = false;
            var source0 = graph.CreateOrGetItem("testdata/source0.cpp", out newItem);
            Assert.AreEqual(false, newItem);
            var source1 = graph.CreateOrGetItem("testdata/source1.cpp", out newItem);
            Assert.AreEqual(false, newItem);
            var testinclude = graph.CreateOrGetItem("testdata/testinclude.h", out newItem);
            Assert.AreEqual(false, newItem);
            var subdir_testinclude = graph.CreateOrGetItem("testdata/subdir/teStinclude.h", out newItem);
            Assert.AreEqual(false, newItem);
            var subdir_inline = graph.CreateOrGetItem("testdata/subdir/inline.inl", out newItem);
            Assert.AreEqual(false, newItem);

            // Check includes in source0.
            Assert.AreEqual(2, source0.Includes.Count);
            Assert.AreEqual(0, source0.Includes[0].IncludeLine.LineNumber); // Different line numbers.
            Assert.AreEqual(5, source0.Includes[1].IncludeLine.LineNumber);
            Assert.AreEqual(subdir_testinclude, source0.Includes[0].IncludedFile);  // But point to the same include.
            Assert.AreEqual(subdir_testinclude, source0.Includes[1].IncludedFile);

            // Check includes in source1.
            Assert.AreEqual(1, source1.Includes.Count);
            Assert.AreEqual(testinclude, source1.Includes[0].IncludedFile);

            // Check includes in testinclude.
            Assert.AreEqual(1, testinclude.Includes.Count);
            Assert.AreEqual(subdir_testinclude, testinclude.Includes[0].IncludedFile);

            // Check includes in subdir_testinclude.
            Assert.AreEqual(1, subdir_testinclude.Includes.Count);
            Assert.AreEqual(subdir_inline, subdir_testinclude.Includes[0].IncludedFile);

            // Check includes in subdir_inline.
            Assert.AreEqual(1, subdir_inline.Includes.Count);
            Assert.AreEqual(null, subdir_inline.Includes[0].IncludedFile);
            Assert.AreEqual(true, subdir_inline.Includes[0].IncludeLine.ContainsActiveInclude);
        }
    }
}
