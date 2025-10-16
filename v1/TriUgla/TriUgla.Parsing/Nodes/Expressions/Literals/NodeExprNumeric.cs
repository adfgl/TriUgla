using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprNumeric : NodeExprLiteralBase
    {
        TuValue _value = TuValue.Nothing;

        public NodeExprNumeric(Token token) : base(token)
        {
        }

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            if (_value.type == EDataType.Nothing)
            {
                string value = Token.value;
                if (double.TryParse(value, out double d))
                {
                    _value = new TuValue(d);
                }
                else
                {
                    throw new CompileTimeException($"Invalid numeric literal '{value}'.", Token);
                }
            }
            return _value;
        }
    }
}
