using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class ProgramNode : INode
    {
        public ProgramNode(Token token, IEnumerable<INode> statements)
        {
            Token = token;
            Statement = statements.ToList();
        }

        public Token Token { get; }
        public IReadOnlyList<INode> Statement { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);
    }
}
