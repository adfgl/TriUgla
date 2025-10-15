using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeExprNumeric : NodeExprLiteralBase
    {
        public NodeExprNumeric(Token token) : base(token)
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
                Value = new TuValue(d);
                return Value;
            }
            throw new CompileTimeException($"Invalid numeric literal '{value}'.", Token);
        }
    }
}
