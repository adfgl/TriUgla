using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprGroup : Expr
    {
        public ExprGroup(Token token, Expr inner) : base(token)
        {
            Inner = inner;
        }

        public Expr Inner { get; }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
