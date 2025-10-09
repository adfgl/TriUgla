using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Nodes.Literals;

namespace TriUgla.Parsing.Nodes
{
    public class NodeFor : INode
    {
        public NodeFor(NodeIdentifierLiteral id, INode range, NodeBlock block)
        {
            Identifier = id;
            Range = range;
            Block = block;
        }

        public NodeIdentifierLiteral Identifier { get; }
        public INode Range { get; }
        public NodeBlock Block { get; }

        public Value Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
