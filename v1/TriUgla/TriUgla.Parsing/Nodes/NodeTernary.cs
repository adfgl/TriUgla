using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeTernary : INode
    {
        public NodeTernary(Token token, INode ifExp, INode thenExp, INode elseExp)
        {
            Token = token;
            IfExp = ifExp;
            ThenExp = thenExp;
            ElseExp = elseExp;
        }

        public Token Token { get; }
        public INode IfExp { get; }
        public INode ThenExp { get; }
        public INode ElseExp { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);
    }
}
