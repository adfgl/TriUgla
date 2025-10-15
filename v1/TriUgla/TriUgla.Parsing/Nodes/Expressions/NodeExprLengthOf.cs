using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprLengthOf : NodeExprBase
    {
        public NodeExprLengthOf(Token token, NodeExprBase tpl) : base(token)
        {
            Tuple = tpl;
        }

        public NodeExprBase Tuple { get; }

        protected override TuValue Evaluate(TuRuntime stack)
        {
            TuValue value = Tuple.Eval(stack);
            if (value.type == EDataType.Tuple)
            {
                return new TuValue(value.AsTuple()!.Values.Count);
            }

            if (Tuple is NodeExprIdentifier id)
            {
                throw new CompileTimeException(
                      $"Cannot obtain number of elements: variable '{id.Name}' " +
                      $"has type '{value.type}', but a tuple was expected.",
                      Token);
            }
            else
            {
                throw new RunTimeException(
                      $"Cannot obtain number of elements from expression of type '{value.type}'. " +
                      $"Expected a tuple value ({{...}}).",
                      Token);
            }
        }
    }
}
