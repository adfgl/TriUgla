using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public class NodeExprString : NodeExprLiteralBase
    {
        public NodeExprString(Token token) : base(token)
        {
        }

        public override TuValue Evaluate(TuRuntime stack)
        {
            return Value;
        }
    }
}
