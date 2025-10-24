using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprUnaryPostfix : Expr
    {
        public ExprUnaryPostfix(Expr left, Token token) : base(token)
        {
            Left = left;
        }

        public Expr Left { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
