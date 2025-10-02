using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Exceptions;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprBinary : NodeExprBase
    {
        public NodeExprBase Left { get; }
        public NodeExprBase Right { get; }

        public NodeExprBinary(NodeExprBase left, Token op, NodeExprBase right) : base(op)
        {
            Left = left;
            Right = right;
        }

        public override Value Evaluate(Scope scope)
        {
            Value left = Left.Evaluate(scope);
            Value right = Right.Evaluate(scope);

            ETokenType op = Token.type;
            if (left.type == EDataType.Numeric && right.type == EDataType.Numeric)
            {
                double l = left.numeric;
                double r = right.numeric;

                double result = 0;
                switch (op)
                {
                    case ETokenType.Plus:
                        result = l + r;
                        break;
                    case ETokenType.Minus:
                        result = l - r;
                        break;
                    case ETokenType.Star:
                        result = l * r;
                        break;
                    case ETokenType.Slash:
                        result = l / r;
                        break;

                    case ETokenType.Power:
                        result = Math.Pow(l, r);
                        break;

                    case ETokenType.Modulo:
                        result = l % r;
                        break;

                    case ETokenType.EqualStrict:
                        return new Value(l == r);

                    case ETokenType.NotEqual:
                        return new Value(l != r);

                    case ETokenType.Less:
                        return new Value(l < r);

                    case ETokenType.LessOrEqual:
                        return new Value(l <= r);

                    case ETokenType.Greater:
                        return new Value(l > r);

                    case ETokenType.GreaterOrEqual:
                        return new Value(l >= r);

                    case ETokenType.And:
                        return new Value(((l != 0.0) && (r != 0.0)) ? 1.0 : 0.0);

                    case ETokenType.Or:
                        return new Value(((l != 0.0) || (r != 0.0)) ? 1.0 : 0.0);

                    default:
                        throw new InvalidOperatorException(Token, left, right);
                }
                return new Value(result);
            }

            throw new InvalidOperatorException(Token, left, right);
        }
    }
}
