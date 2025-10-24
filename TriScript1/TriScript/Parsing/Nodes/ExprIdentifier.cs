using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprIdentifier : Expr
    {
        public ExprIdentifier(Token token) : base(token)
        {
        }

        public Token Id => Token;

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
