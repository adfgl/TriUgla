
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprNumber : Expr
    {
        public ExprNumber(Token token, Value value)
        {
            Token = token;
            Value = value;
        }

        public Token Token { get; }
        public Value Value { get; }

        public override Value Evaluate(Executor rt)
        {
            return Value;
        }
    }
}
