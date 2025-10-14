using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeString : NodeLiteralBase
    {
        public NodeString(Token token) : base(token)
        {
        }

        public override string ToString()
        {
            return Token.value;
        }

        public override TuValue Evaluate(TuStack stack)
        {
            return new TuValue(Token.value);
        }
    }
}
