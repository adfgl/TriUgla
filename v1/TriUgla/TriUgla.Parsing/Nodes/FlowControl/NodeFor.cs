using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeFor : INode
    {
        public NodeFor(Token token, INode id, INode range, NodeBlock block)
        {
            Token = token;
            Identifier = id;
            Range = range;
            Block = block;
        }

        public Token Token { get; }
        public INode Identifier { get; }
        public INode Range { get; }
        public NodeBlock Block { get; }

        public TuValue Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
