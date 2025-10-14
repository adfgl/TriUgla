using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeLengthOf : INode
    {
        public NodeLengthOf(Token hash, INode id)
        {
            Token = hash;
            Id = id;
        }

        public Token Token { get; }
        public INode Id { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);
    }
}
