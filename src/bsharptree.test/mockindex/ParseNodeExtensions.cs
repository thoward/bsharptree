using System;
using System.Collections.Generic;
using bsharptree.example.simpleindex.query.parser;

namespace bsharptree.test.mockindex
{
    public static class ParseNodeExtensions
    {
        public static void PrintParseNode(this ParseNode parseNode)
        {
            var nodes = new Queue<NodeWithDepth>();
            nodes.Enqueue(new NodeWithDepth { Depth = 0, Node = parseNode });

            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();

                Console.Out.WriteLine(string.Empty.PadLeft(node.Depth, ' ') + node.Node.Text);

                foreach (var childNode in node.Node.Nodes)
                    nodes.Enqueue(new NodeWithDepth { Depth = node.Depth + 1, Node = childNode });
            }
        }
        private struct NodeWithDepth
        {
            public ParseNode Node;
            public int Depth;
        }  
    }
}