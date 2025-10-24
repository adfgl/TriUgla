using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprGroup : Expr
    {
        public ExprGroup(Token token, Expr inner) : base(token)
        {
            Inner = inner;
        }

        public Expr Inner { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
