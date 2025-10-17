using TriScript.Data;

namespace TriScript.Parsing.Nodes.Statements
{
    public class NodeStmtExpression : NodeStmtBase
    {
        public NodeStmtExpression(NodeExprBase expr)
        {
            Expr = expr;
        }

        public NodeExprBase Expr { get; }

        public override Value Evaluate(Executor rt)
        {
            Expr.Evaluate(rt);
            return Value.Nothing;
        }
    }
}
