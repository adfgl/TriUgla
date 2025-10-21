using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprReal : ExprLiteral
    {
        public ExprReal(Token token) : base(token)
        {
        }

        public override Value Evaluate(Executor ex)
        {
            string lexeme = ex.Source.GetString(Token.span);
            return new Value(double.Parse(lexeme));
        }
    }
}
