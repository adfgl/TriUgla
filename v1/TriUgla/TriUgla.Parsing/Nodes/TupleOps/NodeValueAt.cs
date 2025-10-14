using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeValueAt : INode
    {
        public NodeValueAt(Token token, INode tuple, INode index)
        {
            Token = token;
            Tuple = tuple;
            Index = index;
        }

        public Token Token { get; }
        public INode Tuple { get; }
        public INode Index { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);
    }
}
