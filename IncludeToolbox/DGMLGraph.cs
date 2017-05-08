using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace IncludeToolbox
{
    /// <summary>
    /// A simple DGML graph file writer.
    /// </summary>
    public class DGMLGraph
    {
        public struct Node
        {
            [XmlAttribute]
            public string Id;
            [XmlAttribute]
            public string Label;
        }

        public struct Link
        {
            [XmlAttribute]
            public string Source;
            [XmlAttribute]
            public string Target;
            [XmlAttribute]
            public string Label;
        }

        public List<Node> Nodes { get; private set; } = new List<Node>();
        public List<Link> Links { get; private set; } = new List<Link>();

        public DGMLGraph()
        {
        }

        public struct Graph
        {
            public Node[] Nodes;
            public Link[] Links;
        }

        public void Serialize(string xmlpath)
        {
            Graph g = new Graph();
            g.Nodes = Nodes.ToArray();
            g.Links = Links.ToArray();

            XmlRootAttribute root = new XmlRootAttribute("DirectedGraph");
            root.Namespace = "http://schemas.microsoft.com/vs/2009/dgml";
            XmlSerializer serializer = new XmlSerializer(typeof(Graph), root);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter xmlWriter = XmlWriter.Create(xmlpath, settings))
            {
                serializer.Serialize(xmlWriter, g);
            }
        }
    }
}
