
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class NodeExprLiteralNumber : NodeExprBase
    {
        public NodeExprLiteralNumber(Token token, Value value)
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
