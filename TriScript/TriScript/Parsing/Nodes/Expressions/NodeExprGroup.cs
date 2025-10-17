using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class NodeExprGroup : NodeExprBase
    {
        public NodeExprGroup(Token open, NodeExprBase expr, Token close)
        {
            Open = open;
            Expr = expr;
            Close = close;
        }

        public Token Open { get; }
        public NodeExprBase Expr { get; }
        public Token Close { get; }

        public override Value Evaluate(Executor rt)
        {
            return Expr.Evaluate(rt);
        }
    }
}
