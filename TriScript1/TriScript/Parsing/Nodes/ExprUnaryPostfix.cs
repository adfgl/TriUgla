using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprUnaryPostfix : Expr
    {
        public ExprUnaryPostfix(Token token) : base(token)
        {
        }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
