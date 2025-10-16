using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprString : NodeExprLiteralBase
    {
        TuValue _value = TuValue.Nothing;

        public NodeExprString(Token token) : base(token)
        {
        }

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            if (_value.type != EDataType.Nothing)
            {
                return _value;
            }
            return new TuValue(Token.value);
        }
    }
}
