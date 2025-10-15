using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeExprLengthOf : NodeBase
    {
        public NodeExprLengthOf(Token token, NodeBase tpl) : base(token)
        {
            Tuple = tpl;
        }

        public NodeBase Tuple { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            TuValue value = Tuple.Evaluate(stack);
            if (value.type == EDataType.Tuple)
            {
                Value = new TuValue(value.AsTuple()!.Values.Count);
                return value;
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
