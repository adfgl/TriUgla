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
            if (op == ETokenType.EqualEqual) return new TuValue(left == right);
            if (op == ETokenType.NotEqual) return new TuValue(left != right);

            if (left.type == EDataType.Text || right.type == EDataType.Text)
            {
                return left + right;
            }
            else if (left.type.IsNumeric() && right.type.IsNumeric())
            {
                switch (op)
                {
                    case ETokenType.Minus: return left - right;
                    case ETokenType.Plus: return left + right;
                    case ETokenType.Star: return left * right;
                    case ETokenType.Slash: return left / right;
                    case ETokenType.Modulo: return left % right;
                    case ETokenType.Power: return left ^ right;
                    
                    case ETokenType.Less: return left < right;
                    case ETokenType.Greater: return left > right;
                    case ETokenType.LessOrEqual: return left <= right;
                    case ETokenType.GreaterOrEqual: return left >= right;
                    default:
                        throw new CompileTimeException($"Invalid operator.", Token);
                }
            }
            throw new Exception($"Unsupported binary operation '{Token.value}'.");
        }

        TuValue EvaluateOr(TuRuntime stack)
        {
            TuValue left = Left.Evaluate(stack);
            if (left.AsBoolean()) return new TuValue(true);

            TuValue right = Right.Evaluate(stack);
            return new TuValue(right.AsBoolean());
        }

        TuValue EvaluateAnd(TuRuntime stack)
        {
            TuValue left = Left.Evaluate(stack);
            if (!left.AsBoolean()) return new TuValue(false);

            TuValue right = Right.Evaluate(stack);
            return new TuValue(right.AsBoolean());
        }
    }
}
