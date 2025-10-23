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

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
