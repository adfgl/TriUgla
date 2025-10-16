using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprLengthOf : NodeExprBase
    {
        public NodeExprLengthOf(Token token, NodeExprBase tpl) : base(token)
        {
            Tuple = tpl;
        }

        public NodeExprBase Tuple { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            TuValue value = Tuple.Evaluate(rt);
            if (value.type == EDataType.Tuple)
            {
                return new TuValue(value.AsTuple()!.Values.Count);
            }

            throw new RunTimeException(
                               $"Cannot obtain number of elements from expression of type '{value.type}'. " +
                               $"Expected a tuple value ({{...}}).",
                               Token);
        }
    }
}
