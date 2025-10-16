using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprBinary : NodeExprBase
    {
        public NodeExprBinary(NodeExprBase left, Token op, NodeExprBase right) : base(op)
        {
            Left = left;
            Right = right;
        }

        public NodeExprBase Left { get; }
        public Token Operation => Token;
        public NodeExprBase Right { get; }

        public override string ToString()
        {
            return $"{Left} {Token.value} {Right}";
        }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            ETokenType op = Operation.type;
            if (op == ETokenType.Or) return EvaluateOr(rt);
            if (op == ETokenType.And) return EvaluateAnd(rt);

            TuValue left = Left.Evaluate(rt);
            TuValue right = Right.Evaluate(rt);

            if (left.type.IsNumeric() && right.type.IsNumeric())
            {
                TuValue result = EvaluateNumericNumeric(left, right);
                if (left.type == EDataType.Real || right.type == EDataType.Real)
                {
                    return new TuValue(result.AsNumeric());
                }
                return result;
            }

            if (op == ETokenType.Plus) return left + right;

            if (op == ETokenType.EqualEqual)
            {
                return new TuValue(left == right);
            }
            if (op == ETokenType.NotEqual)
            {
                return new TuValue(left != right);
            }
            throw new Exception($"Unsupported binary operation '{Token.value}'.");
        }

        TuValue CheckForNothing(TuRuntime stack, NodeBase node)
        {
            TuValue value = node.Evaluate(stack);
            if (value.type == EDataType.Nothing)
            {
                throw new RunTimeException($"Should evaluate to '{EDataType.Real}' but got '{EDataType.Nothing}'", node.Token);
            }
            return value;
        }

        TuValue EvaluateOr(TuRuntime stack)
        {
            TuValue left = CheckForNothing(stack, Left);
            if (left.AsBoolean()) return new TuValue(true);

            TuValue right = CheckForNothing(stack, Right);
            return new TuValue(right.AsBoolean());
        }

        TuValue EvaluateAnd(TuRuntime stack)
        {
            TuValue left = CheckForNothing(stack, Left);
            if (!left.AsBoolean()) return new TuValue(false);

            TuValue right = CheckForNothing(stack, Right);
            return new TuValue(right.AsBoolean());
        }

        TuValue EvaluateNumericNumeric(TuValue left, TuValue right)
        {
            ETokenType op = Operation.type;

            double l = left.AsNumeric();
            double r = right.AsNumeric();

            double dbl = op switch
            {
                ETokenType.Minus => l - r,
                ETokenType.Plus => l + r,
                ETokenType.Slash => l / r,
                ETokenType.Star => l * r,

                ETokenType.Modulo => l % r,
                ETokenType.Power => Math.Pow(l, r),

                _ => double.NaN
            };

            if (!double.IsNaN(dbl))
            {
                if (left.type == EDataType.Integer && right.type == EDataType.Integer)
                {
                    return new TuValue((int)dbl);
                }
                return new TuValue(dbl);
            }

            bool bl = op switch
            {
                ETokenType.Less => l < r,
                ETokenType.Greater => l > r,
                ETokenType.LessOrEqual => l <= r,
                ETokenType.GreaterOrEqual => l >= r,

                ETokenType.EqualEqual => l == r,
                ETokenType.NotEqual => l != r,

                _ => throw new CompileTimeException(
                    $"Unsupported binary operation '{Token.value}'.",
                    Token)
            };
            return new TuValue(bl);
        }
    }
}
