using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprUnaryPrefix : Expr
    {
        public ExprUnaryPrefix(Token token, Expr right) : base(token)
        {
            Right = right;
        }

        public Expr Right { get; }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
