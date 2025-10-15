using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprBinary : NodeExprBase
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

        public override TuValue Evaluate(TuRuntime stack)
        {
            ETokenType op = Operation.type;
            if (op == ETokenType.Or) return EvaluateOr(stack);
            if (op == ETokenType.And) return EvaluateAnd(stack);

            TuValue left = Left.Evaluate(stack);
            TuValue right = Right.Evaluate(stack);

            if (left.type == EDataType.Numeric &&
                right.type == EDataType.Numeric)
            {
                return EvaluateNumericNumeric();
            }

            if (op == ETokenType.Plus && (left.type == EDataType.String || right.type == EDataType.String))
            {
                return new TuValue(left.AsString() + right.AsString());
            }

            if (left.type == EDataType.Tuple &&
                right.type == EDataType.Tuple)
            {
                return EvaluateTupleTuple();
            }

            if (left.type == EDataType.Tuple && right.type == EDataType.Numeric ||
                right.type == EDataType.Tuple && left.type == EDataType.Numeric)
            {
                return EvaluateTupleNumeric();
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

        TuValue EvaluateNumericNumeric()
        {
            ETokenType op = Operation.type;

            if (op == ETokenType.Slash)
            {
                CheckDivisionByZero(Right);
            }

            double l = Left.Value.AsNumeric();
            double r = Right.Value.AsNumeric();

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


        TuValue EvaluateTupleTuple()
        {
            List<double> lt = Left.Value.AsTuple()!.Values;
            ETokenType op = Operation.type;
            List<double> rt = Right.Value.AsTuple()!.Values;

            if (lt.Count != rt.Count)
            {
                throw new RunTimeException("Tuple sizes must match for element-wise operation", Token);
            }

            Func<double, double, double> f = op switch
            {
                ETokenType.Plus => (a, b) => a + b,
                ETokenType.Minus => (a, b) => a - b,
                ETokenType.Star => (a, b) => a * b,
                ETokenType.Slash => (a, b) => a / b,
                ETokenType.Modulo => (a, b) => a % b,
                ETokenType.Power => Math.Pow,
                _ => throw new CompileTimeException(
                    $"Unsupported binary operation '{Token.value}'.",
                    Operation)
            };

            TuTuple tpl;
            if (op == ETokenType.Slash)
            {
                double[] result = new double[lt.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    double l = lt[i];
                    double r = rt[i];
                    if (r == 0)
                    {
                        throw new RunTimeException($"Tuple element {ToOrdinal(i + 1)} evaluated to zero, resulting in division by zero.", Token);
                    }
                    result[i] = l / r;
                }
                tpl = new TuTuple(result);
            }
            else
            {
                tpl = new TuTuple(lt.Zip(rt, f));
            }
            return new TuValue(tpl);
        }

        public TuValue EvaluateTupleNumeric()
        {
            NodeBase tupleNode = Left, scalarNode = Right;
            if (Right.Value.type == EDataType.Tuple)
            {
                tupleNode = Right;
                scalarNode = Left;
            }

            TuTuple tuple = Left.Value.AsTuple()!;
            ETokenType op = Operation.type;
            double scalar = Right.Value.AsNumeric();

            if (op == ETokenType.Slash)
            {
                CheckDivisionByZero(scalarNode);
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
            if (fb is not null)
            {
                return new TuValue(tuple.All(fb));
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
            if (fa is not null)
            {
                return new TuValue(new TuTuple(tuple.Select(fa)));
            }

            throw new CompileTimeException(
                $"Unsupported binary operation '{Token.value}'.",
                Operation);
        }
    }
}
