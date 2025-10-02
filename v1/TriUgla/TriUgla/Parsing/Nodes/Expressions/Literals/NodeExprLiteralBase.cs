using TriUgla.Parsing.Exceptions;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public abstract class NodeExprLiteralBase : NodeExprBase
    {
        protected Value _value { get; set; }

        protected NodeExprLiteralBase(Token token, ETokenType expected) : base(token)
        {
            if (token.type != expected)
            {
                throw new UnexpectedTokenException(token, expected);
            }
            _value = token.value;
        }

        public Value Value => _value;

        public override Value Evaluate(Scope scope)
        {
            return _value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
