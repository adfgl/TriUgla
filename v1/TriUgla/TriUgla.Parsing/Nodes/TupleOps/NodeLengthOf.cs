using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeLengthOf : NodeBase
    {
        public NodeLengthOf(Token token, NodeBase tpl) : base(token)
        {
            Tuple = tpl;
        }

        public NodeBase Tuple { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue value = Tuple.Evaluate(stack);
            if (value.type == EDataType.Tuple)
            {
                return new TuValue(value.AsTuple()!.Values.Count);
            }

            if (Tuple is NodeIdentifier id)
            {
                throw new CompiletimeException(
                      $"Cannot obtain number of elements: variable '{id.Name}' " +
                      $"has type '{value.type}', but a tuple was expected.",
                      Token);
            }
            else
            {
                throw new RuntimeException(
                      $"Cannot obtain number of elements from expression of type '{value.type}'. " +
                      $"Expected a tuple value ({{...}}).",
                      Token);
            }
        }
    }
}
