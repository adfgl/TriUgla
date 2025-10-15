using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeExprString : NodeExprLiteralBase
    {
        public NodeExprString(Token token) : base(token)
        {
        }

        public override string ToString()
        {
            return Token.value;
        }

        public override TuValue Evaluate(TuStack stack)
        {
            Value = new TuValue(Token.value);
            return Value;
        }
    }
}
