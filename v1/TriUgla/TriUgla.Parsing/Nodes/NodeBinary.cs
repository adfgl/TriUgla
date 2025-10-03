using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeBinary : INode
    {
        public NodeBinary(INode left, Token op, INode right)
        {
            Left = left;
            Operation = op;
            Right = right;
        }

        public INode Left { get; }
        public Token Operation { get; }
        public INode Right { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
