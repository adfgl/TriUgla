using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprBoolean : ExprLiteral
    {
        public ExprBoolean(Token token) : base(token) { }

        public override Value Evaluate(Executor ex)
        {
            bool result;
            if (Token.type == ETokenType.True)
            {
                result = true;
            }
            else if (Token.type == ETokenType.False)
            {
                result = false;
            }
            else
            {
                throw new ArgumentException();
            }
            return new Value(result);
        }
    }
}
