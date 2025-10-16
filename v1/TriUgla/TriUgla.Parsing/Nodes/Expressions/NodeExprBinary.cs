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

        protected override TuValue Eval(TuRuntime stack)
        {
            ETokenType op = Operation.type;
            if (op == ETokenType.Or) return EvaluateOr(stack);
            if (op == ETokenType.And) return EvaluateAnd(stack);

            TuValue left = Left.Evaluate(stack);
            TuValue right = Right.Evaluate(stack);

            if (left.type == EDataType.Numeric &&
                right.type == EDataType.Numeric)
            {
                return EvaluateNumericNumeric(left, right);
            }

            if (op == ETokenType.Plus && (left.type == EDataType.Text || right.type == EDataType.Text))
            {
                return new TuValue(left.AsString() + right.AsString());
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

        TuValue CheckForNothing(TuRuntime stack, NodeBase node)
        {
            TuValue value = node.Evaluate(stack);
            if (value.type == EDataType.Nothing)
            {
                throw new RunTimeException($"Should evaluate to '{EDataType.Numeric}' but got '{EDataType.Nothing}'", node.Token);
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

            if (op == ETokenType.Slash)
            {
                CheckDivisionByZero(Right, right);
            }

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
