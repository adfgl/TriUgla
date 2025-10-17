using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Assignment
{
    public class NodeExprAssignmentAt : NodeExprBase
    {
        public NodeExprAssignmentAt(NodeExprIndex at, Token op, NodeExprBase expr) : base(op)
        {
            At = at;
            Expression = expr;
        }

        public NodeExprIndex At { get; }
        public NodeExprBase Expression { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            TuValue current = At.Evaluate(rt);
            TuValue value = Expression.Evaluate(rt);

            if (current.type == EDataType.Float &&
                value.type == EDataType.Integer)
            {
                value = new TuValue((int)value.AsNumeric());
            }

            if (current.type != value.type)
            {
                throw new Exception();
            }

            TuValue toAssign;
            switch (Token.type)
            {
                case ETokenType.Equal:
                    toAssign = value;
                    break;

                case ETokenType.MinusEqual:
                    toAssign = current - value;
                    break;

                case ETokenType.PlusEqual:
                    toAssign = current + value;
                    break;

                case ETokenType.StarEqual:
                    toAssign = current * value;
                    break;

                case ETokenType.SlashEqual:
                    toAssign = current / value;
                    break;

                case ETokenType.ModuloEqual:
                    toAssign = current % value;
                    break;

                case ETokenType.PowerEqual:
                    toAssign = current ^ value;
                    break;

                default:
                    throw new Exception();
            }

            At.Tuple[At.Index] = toAssign;
            return value;
        }
    }
}
