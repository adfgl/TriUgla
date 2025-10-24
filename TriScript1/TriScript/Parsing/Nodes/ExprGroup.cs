using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprGroup : Expr
    {
        public ExprGroup(Token open, Expr inner, Token close) : base(open)
        {
            Inner = inner;
            Close = close;
        }

        public Token Open => Token;
        public Expr Inner { get; }
        public Token Close { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
