using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprNumeric : Expr
    {
        public ExprNumeric(Token token, Value value) : base(token)
        {
            Value = value;
        }

        public Value Value { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
