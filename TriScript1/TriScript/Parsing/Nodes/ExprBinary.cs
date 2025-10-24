using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprBinary : Expr
    {
        public ExprBinary(Expr left, Token token, Expr right) : base(token)
        {
            Left = left;
            Right = right;
        }

        public Expr Left { get; }
        public Token Operator => Token;
        public Expr Right { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
