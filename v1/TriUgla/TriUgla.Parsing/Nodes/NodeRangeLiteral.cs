using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeRangeLiteral : INode
    {
        public NodeRangeLiteral(INode from, INode to, INode? by)
        {
            From = from;
            To = to;
            By = by;
        }

        public INode From { get; }
        public INode To { get; }
        public INode? By { get; }

        public Value Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }

    }
}
