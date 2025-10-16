using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprGroup : NodeExprBase
    {
        public NodeExprGroup(Token open, NodeExprBase exp, Token close) : base(open)
        {
            Expression = exp;
            Close = close;
        }

        public Token Open => Token;
        public NodeExprBase Expression { get; }
        public Token Close { get; }

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            TuValue value = Expression.Evaluate(stack);
            if (value.type == EDataType.Nothing)
            {
                throw new RunTimeException(
                    "Grouped expression evaluated to 'Nothing'.",
                    Expression.Token);
            }
            return value;
        }

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}
