using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeNumeric : NodeBase
    {
        public NodeNumeric(Token token) : base(token)
        {
        }

        public override string ToString()
        {
            return Token.value;
        }

        public override TuValue Evaluate(TuStack stack)
        {
            string value = Token.value;
            if (double.TryParse(value, out double d))
            {
                return new TuValue(d);
            }
            throw new Exception($"Invalid numeric literal '{value}'.");
        }
    }
}
