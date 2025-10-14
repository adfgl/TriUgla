using System.Runtime.ConstrainedExecution;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Nodes.TupleOps;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeAssignment : NodeBase
    {
        public NodeAssignment(NodeBase id, Token op, NodeBase expression) : base(op)
        {
            Assignee = id;
            Expression = expression;
        }

        public NodeBase Assignee { get; }
        public ETokenType Operation => Token.type;
        public NodeBase Expression { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            if (Expression is NodeValueAt valueAt)
            {
                TuValue curr = valueAt.Evaluate(stack);
                int i = valueAt.Index;
                TuTuple tpl = valueAt.Tuple;
                TuValue value = Expression.Evaluate(stack);

                if (curr.type != value.type)
                {
                    throw new Exception("Type mismatch");
                }
                tpl.Values[i] = value.AsNumeric();
                return value;
            }
            
            if (Expression is NodeIdentifier id)
            {
                TuValue curr = id.Evaluate(stack);
                TuValue value = Expression.Evaluate(stack);

                if (curr.type != EDataType.Nothing &&
                    curr.type != value.type)
                {
                    throw new Exception("Type mismatch");
                }

                Variable variable = stack.Current.Get(id.Name)!;

                if (Operation == ETokenType.Equal)
                {
                    variable.Value = value;
                    return value;
                }

                if (value.type == EDataType.Tuple)
                {
                    if (curr.type == EDataType.Nothing)
                    {
                        throw new Exception("Invalid operation for unassigned varialble.");
                    }

                    if (Operation == ETokenType.PlusEqual)
                    {
                        List<double> values = new(variable.Value.AsTuple()!);
                        values.AddRange(value.AsTuple()!);

                        variable.Value = new TuValue(new TuTuple(values));
                        return variable.Value;
                    }
                    throw new Exception();
                }

                if (value.type == EDataType.Numeric)
                {
                    if (curr.type == EDataType.Nothing)
                    {
                        throw new Exception("Invalid operation for unassigned varialble.");
                    }

                    double l = curr.AsNumeric();
                    double r = value.AsNumeric();
                    double n = Operation switch
                    {
                        ETokenType.PlusEqual => l + r,
                        ETokenType.MinusEqual => l - r,
                        ETokenType.StarEqual => l * r,
                        ETokenType.SlashEqual => r == 0 ? throw new DivideByZeroException() : l / r,
                        ETokenType.ModuloEqual => l % r,
                        ETokenType.PowerEqual => Math.Pow(l, r),
                        _ => throw new Exception($"Unsupported operation {Operation}")
                    };

                    variable.Value = new TuValue(n);
                    return variable.Value;
                }

            }

            throw new Exception();
        }
    }
}
