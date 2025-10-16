using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtExpression : NodeStmtBase
    {
        public NodeStmtExpression(Token token, NodeExprBase expr) : base(token)
        {
            Expr = expr;
        }

        public NodeExprBase Expr { get; }

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            return Expr.Evaluate(stack);
        }
    }
}
