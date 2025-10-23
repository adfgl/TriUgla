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

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
