using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Statements.Conditional
{
    public class NodeStmtElse : NodeStmtBase
    {
        public NodeStmtBase ElseBody { get; }

        public NodeStmtElse(Token token, IEnumerable<NodeStmtBase> elseBody) : base(token)
        {
            ElseBody = new NodeStmtBlock(elseBody.ToArray());
        }

        public override Value Evaluate(Scope scope)
        {
            return ElseBody.Evaluate(scope);
        }
    }
}
