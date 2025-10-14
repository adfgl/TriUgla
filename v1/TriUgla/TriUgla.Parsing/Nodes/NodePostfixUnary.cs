using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodePostfixUnary : NodeBase
    {
        public NodePostfixUnary(Token op, NodeBase exp) : base(op)
        {
            Expression = exp;
        }

        public Token Operation => Token;
        public NodeBase Expression { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            ETokenType op = Operation.type;
            if (op != ETokenType.PlusPlus && op != ETokenType.MinusMinus)
            {
                throw new Exception($"Unsupported postfix op '{Operation.value}'.");
            }

            if (Expression is not NodeIdentifier id)
            {
                throw new Exception($"Postfix {Operation.value} requires an identifier");
            }

            TuValue value = id.Evaluate(stack);
            if (value.type != EDataType.Numeric)
            {
                throw new Exception($"Postfix {Operation.value} requires numeric variable");
            }

            Variable v = stack.Current.Get(id.Name)!;
            double cur = v.Value.AsNumeric();
            double next = op == ETokenType.PlusPlus ? cur + 1 : cur - 1;

            v.Value = new TuValue(next);
            return new TuValue(cur);

        }
    }
}
