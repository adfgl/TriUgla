using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprAssignment : Expr
    {
        public ExprAssignment(Token token, Expr value) : base(token)
        {
            Value = value;
        }

        public Expr Value { get; }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
