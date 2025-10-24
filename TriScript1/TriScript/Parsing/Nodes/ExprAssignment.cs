using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprAssignment : Expr
    {
        public ExprAssignment(Token token, Expr value) : base(token)
        {
            Value = value;
        }

        public Token Assignee => Token;
        public Expr Value { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
