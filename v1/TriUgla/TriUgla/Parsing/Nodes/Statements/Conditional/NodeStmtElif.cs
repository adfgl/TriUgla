using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Statements.Conditional
{
    public class NodeStmtElif : NodeStmtBase
    {
        public NodeExprBase Condition { get; }
        public NodeStmtBlock ElifBody { get; }

        public NodeStmtElif(Token token, NodeExprBase condition, IEnumerable<NodeStmtBase> elifBody) : base(token)
        {
            Condition = condition;
            ElifBody = new NodeStmtBlock(elifBody.ToArray());
        }

        public override Value Evaluate(Scope scope)
        {
            return ElifBody.Evaluate(scope);
        }
    }
}
