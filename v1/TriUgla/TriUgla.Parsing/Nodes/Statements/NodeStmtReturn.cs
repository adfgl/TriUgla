using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtReturn : NodeStmtBase
    {
        public NodeStmtReturn(Token token, NodeExprBase? expr) : base(token)
        {
        }

        public NodeExprBase? Expr { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            if (Expr == null) return TuValue.Nothing;
            rt.Flow.SignalReturn(Expr.Evaluate(rt));
            return rt.Flow.ReturnValue;
        }
    }
}
