using System.Linq.Expressions;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeExprBase : NodeBase
    {
        protected NodeExprBase(Token token) : base(token)
        {
        }

        protected TuValue AssignToElement(TuRuntime stack, NodeExprValueAt valueAt, NodeExprBase expr)
        {
            TuValue curr = valueAt.Evaluate(stack);
            if (curr.type != EDataType.Tuple)
            {
                throw new RunTimeException($"Expected '{EDataType.Tuple}' but got '{curr.type}'", valueAt.TupleExp.Token);
            }

            TuTuple tpl = curr.AsTuple();
            TuValue value = expr.Evaluate(stack);
            if (value.type != curr.type)
            {
                throw new RunTimeException($"Expected '{curr.type}' but got '{value.type}'", expr.Token);
            }
            tpl.Values[valueAt.Index] = value.AsNumeric();
            return value;
        }
    }
}
