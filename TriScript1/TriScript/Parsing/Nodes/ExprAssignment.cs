using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprAssignment : Expr
    {
        public ExprAssignment(ExprLiteralSymbol assignee, Token token, Expr value) : base(token)
        {
            Assignee = assignee;
            Value = value;
        }

        public ExprLiteralSymbol Assignee { get; }
        public Token Assign => Token;
        public Expr Value { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }

    }
}
