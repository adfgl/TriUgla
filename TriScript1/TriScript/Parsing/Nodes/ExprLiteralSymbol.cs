using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprLiteralSymbol : ExprLiteralBase
    {
        public ExprLiteralSymbol(Token token, Value value) : base(token, value)
        {
        }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
