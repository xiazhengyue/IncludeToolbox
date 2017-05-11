using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IncludeToolbox;
using System.IO;
using IncludeToolbox.IncludeToolbox;

namespace Tests
{
    [TestClass]
    public class DGML
    {
        [TestMethod]
        public void WriteGraph()
        {
            string filenameTestOutput = "testdata/test.dgml";
            string filenameComparision= "testdata/expected.dgml";
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
    }
}
