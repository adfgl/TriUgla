using TriScript.Scanning;
using TriScript.UnitHandling;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprWithUnit : Expr
    {
        public ExprWithUnit(Token token, Expr inner, DimEval eval) : base(token)
        {
            Inner = inner;
            Eval = eval;
        }

        public Expr Inner { get; }
        public DimEval Eval { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
