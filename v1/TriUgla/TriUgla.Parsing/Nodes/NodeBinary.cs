using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeBinary : NodeBase
    {
        public NodeBinary(NodeBase left, Token op, NodeBase right) : base(op)
        {
            Left = left;
            Right = right;
        }

        public NodeBase Left { get; }
        public ETokenType Operation => Token.type;
        public NodeBase Right { get; }

        public override string ToString()
        {
            return $"{Left} {Token.type} {Right}";
        }

        public override TuValue Evaluate(TuStack stack)
        {
            ETokenType op = Operation;
            TuValue left, right;
            if (op == ETokenType.Or)
            {
                left = Left.Evaluate(stack);
                if (left.AsBoolean()) return new TuValue(true);

                right = Right.Evaluate(stack);
                return new TuValue(right.AsBoolean());
            }

            if (op == ETokenType.And)
            {
                left = Left.Evaluate(stack);
                if (!left.AsBoolean()) return new TuValue(false);

                right = Right.Evaluate(stack);
                return new TuValue(right.AsBoolean());
            }

            left = Left.Evaluate(stack);
            right = Right.Evaluate(stack);

            if (left.type == EDataType.Numeric && right.type == EDataType.Numeric)
            {
                double l = left.AsNumeric();
                double r = right.AsNumeric();
                switch (op)
                {
                    case ETokenType.Minus: return new TuValue(l - r);
                    case ETokenType.Plus: return new TuValue(l + r);
                    case ETokenType.Slash: return new TuValue(l / r);
                    case ETokenType.Star: return new TuValue(l * r);

                    case ETokenType.Modulo: return new TuValue(l % r);
                    case ETokenType.Power: return new TuValue(Math.Pow(l, r));

                    case ETokenType.Less: return new TuValue(l < r);
                    case ETokenType.Greater: return new TuValue(l > r);
                    case ETokenType.LessOrEqual: return new TuValue(l <= r);
                    case ETokenType.GreaterOrEqual: return new TuValue(l >= r);

                    case ETokenType.EqualEqual: return new TuValue(l == r);
                    case ETokenType.NotEqual: return new TuValue(l != r);
                }
            }

            if (op == ETokenType.Plus && (left.type == EDataType.String || right.type == EDataType.String))
            {
                return new TuValue(left.AsString() + right.AsString());
            }

            bool lIsTuple = left.type == EDataType.Tuple;
            bool rIsTuple = right.type == EDataType.Tuple;
            bool lIsNum = left.type == EDataType.Numeric;
            bool rIsNum = right.type == EDataType.Numeric;

            if (lIsTuple && rIsTuple)
            {
                List<double> lt = left.AsTuple()!.Values;
                List<double> rt = right.AsTuple()!.Values;
                if (lt.Count != rt.Count)
                {
                    throw new Exception("Tuple sizes must match for element-wise operation");
                }

                Func<double, double, double> f = op switch
                {
                    ETokenType.Plus => (a, b) => a + b,
                    ETokenType.Minus => (a, b) => a - b,
                    ETokenType.Star => (a, b) => a * b,
                    ETokenType.Slash => (a, b) => a / b,
                    ETokenType.Modulo => (a, b) => a % b,
                    ETokenType.Power => Math.Pow,
                    _ => throw new Exception($"Unsupported operator {op}")
                };
            }

            if (lIsTuple && rIsNum || lIsNum && rIsTuple)
            {
                TuTuple t = (lIsTuple ? left : right).AsTuple()!;
                double scalar = (lIsNum ? left : right).AsNumeric();

                if (op == ETokenType.Slash && scalar == 0)
                {
                    throw new DivideByZeroException();
                }

                Func<double, bool>? fb = op switch
                {
                    ETokenType.EqualEqual => (a) => a == scalar,
                    ETokenType.NotEqual => (a) => a != scalar,
                    ETokenType.Less => (a) => a < scalar,
                    ETokenType.LessOrEqual => (a) => a <= scalar,
                    ETokenType.Greater => (a) => a > scalar,
                    ETokenType.GreaterOrEqual => (a) => a >= scalar,
                    _ => null
                };

                if (fb != null)
                {
                    return new TuValue(t.All(fb));
                }

                Func<double, double>? fa = op switch
                {
                    ETokenType.Plus => (a) => a + scalar,
                    ETokenType.Minus => (a) => a - scalar,
                    ETokenType.Star => (a) => a * scalar,
                    ETokenType.Slash => (a) => a / scalar,
                    ETokenType.Modulo => (a) => a % scalar,
                    ETokenType.Power => (a) => Math.Pow(a, scalar),
                    _ => null
                };

                if (fa != null)
                {
                    return new TuValue(new TuTuple(t.Select(fa)));
                }
            }

            if (op == ETokenType.EqualEqual)
            {
                return new TuValue(left.AsString() == right.AsString());
            }
            if (op == ETokenType.NotEqual)
            {
                return new TuValue(left.AsString() != right.AsString());
            }
            throw new Exception($"Unsupported binary operation '{Token.value}'.");
        }

    }
}
