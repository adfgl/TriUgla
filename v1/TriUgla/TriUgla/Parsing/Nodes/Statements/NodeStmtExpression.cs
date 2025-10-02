using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtExpression : NodeStmtBase
    {
        public NodeExprBase Expression { get; }

        public NodeStmtExpression(Token token, NodeExprBase expression) : base(token)
        {
            Expression = expression;
        }

        public override Value Evaluate(Scope scope)
        {
            return Expression.Evaluate(scope);
        }
    }
}
