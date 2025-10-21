using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprBoolean : ExprLiteral
    {
        public ExprBoolean(Token token) : base(token) { }

        public override Value Evaluate(Executor ex)
        {
            return new Value(Token.type == ETokenType.True);
        }
    }
}
