using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprUnaryPrefix : Expr
    {
        public ExprUnaryPrefix(Token token, Expr right) : base(token)
        {
            Right = right;
        }

        public Token Operator => Token;
        public Expr Right { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
