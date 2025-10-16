using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprNumeric : NodeExprLiteralBase
    {
        public NodeExprNumeric(Token token) : base(token)
        {
        }

        protected override TuValue Eval(TuRuntime stack)
        {
            string value = Token.value;
            if (double.TryParse(value, out double d))
            {
                return new TuValue(d);
            }
            throw new CompileTimeException($"Invalid numeric literal '{value}'.", Token);
        }
    }
}
