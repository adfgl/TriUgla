using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodePostfixUnary : INode
    {
        public NodePostfixUnary(Token op, INode exp)
        {
            Token = op;
            Expression = exp;
        }

        public Token Token { get; }
        public INode Expression { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);
    }
}
