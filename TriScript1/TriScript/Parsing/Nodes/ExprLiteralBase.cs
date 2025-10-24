using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class ExprLiteralBase : Expr
    {
        protected ExprLiteralBase(Token token, Value value) : base(token)
        {
            Value = value;
        }

        public Value Value { get; }
    }
}
