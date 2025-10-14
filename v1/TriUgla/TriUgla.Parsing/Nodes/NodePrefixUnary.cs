using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodePrefixUnary : NodeBase
    {
        public NodePrefixUnary(Token op, NodeBase exp) : base(op)
        {
            Expression = exp;
        }

        public Token Operation => Token;
        public NodeBase Expression { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            ETokenType op = Operation.type;

            TuValue value;
            if (op == ETokenType.PlusPlus || op == ETokenType.MinusMinus)
            {
                if (Expression is not NodeIdentifier id)
                {
                    throw new Exception($"Prefix {Operation.value} requires an identifier");
                }

                value = id.Evaluate(stack);
                if (value.type != EDataType.Numeric)
                {
                    throw new Exception($"Postfix {Operation.value} requires numeric variable");
                }

                Variable v = stack.Current.Get(id.Name)!;
                double cur = v.Value.AsNumeric();
                double next = op == ETokenType.PlusPlus ? cur + 1 : cur - 1;
                v.Value = new TuValue(next);
                return v.Value;
            }

            value = Expression.Evaluate(stack);
            if (op == ETokenType.Not)
            {
                return new TuValue(!value.AsBoolean());
            }

            if (value.type != EDataType.Numeric)
            {
                throw new Exception($"Expected numeric but got {value.type}");
            }

            if (op == ETokenType.Plus)
            {
                return value;
            }

            if (op == ETokenType.Minus)
            {
                return new TuValue(-value.AsNumeric());
            }

            throw new Exception($"Unsupported unary operation '{Operation.value}'.");
        }
    }
}
