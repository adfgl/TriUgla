using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprUnaryPrefix : Expr
    {
        public ExprUnaryPrefix(Token token) : base(token)
        {
        }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
