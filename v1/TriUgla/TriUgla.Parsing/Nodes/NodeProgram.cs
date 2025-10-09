using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;

namespace TriUgla.Parsing.Nodes
{
    public class NodeProgram : INode
    {
        public NodeProgram(NodeBlock block)
        {
            Block = block;
        }

        public NodeBlock Block { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
