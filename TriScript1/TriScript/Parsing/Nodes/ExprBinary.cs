using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprBinary : Expr
    {
        public ExprBinary(Token token, Expr left, Expr right) : base(token)
        {
            Left = left;
            Right = right;
        }

        public Expr Left { get; }
        public Expr Right { get; }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
