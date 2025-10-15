using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeExprString : NodeExprLiteralBase
    {
        public NodeExprString(Token token) : base(token)
        {
        }

        public override TuValue Evaluate(TuRuntime stack)
        {
            Value = new TuValue(Token.value);
            return Value;
        }
    }
}
