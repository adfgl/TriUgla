using System.Runtime.CompilerServices;
using System.Threading;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Scanning;
using Range = TriUgla.Parsing.Compiling.TuRange;

namespace TriUgla.Parsing.Compiling
{
    public class Evaluator : INodeVisitor
    {
        Stack _stack = new Stack();

        public Stack Stack => _stack;

        public TuValue Visit(NodeNumericLiteral n)
        {
            if (double.TryParse(n.Token.value, out double d))
            {
                return new TuValue(d);
            }

            throw new Exception();
        }

        public TuValue Visit(NodeStringLiteral n)
        {
            TuValue value = new TuValue(n.Token.value);
            return value;
        }

        public static bool ValidIdentifier(string id)
        {
            return true;
        }

        public TuValue Visit(NodeIdentifierLiteral n)
        {
            if (!ValidIdentifier(n.Token.value))
            {
                throw new Exception($"Invalid id '{n.Token.value}'");
            }

            Variable? v = _stack.Current.Get(n.Token);
            if (v is null)
            {
                throw new Exception();
            }
            return v.Value;
        }

        public TuValue Visit(NodeUnary n)
        {
            TuValue value = n.Expression.Accept(this);
            ETokenType op = n.Token.type;
            if (value.type == EDataType.Numeric)
            {
                switch (op)
                {
                    case ETokenType.Minus: return new TuValue(-value.AsNumeric());
                    case ETokenType.Plus:  return value; 
                }
            }

            if (op == ETokenType.Not)
            {
                return new TuValue(!value.AsBoolean());
            }
            throw new Exception();
        }

        public TuValue Visit(NodeBinary n)
        {
            TuValue left = n.Left.Accept(this);
            TuValue right = n.Right.Accept(this);

            ETokenType op = n.Token.type;

            if (left.type == EDataType.Numeric && right.type == EDataType.Numeric)
            {
                double l = left.AsNumeric();
                double r = right.AsNumeric();

                switch (op)
                {
                    case ETokenType.Minus:          return new TuValue(l - r);
                    case ETokenType.Plus:           return new TuValue(l + r);
                    case ETokenType.Slash:          return new TuValue(l / r);
                    case ETokenType.Star:           return new TuValue(l * r);
                    case ETokenType.Modulo:         return new TuValue(l % r);
                    case ETokenType.Power:          return new TuValue(Math.Pow(l, r));
                    case ETokenType.Less:           return new TuValue(l < r);
                    case ETokenType.Greater:        return new TuValue(l > r);
                    case ETokenType.LessOrEqual:    return new TuValue(l <= r);
                    case ETokenType.GreaterOrEqual: return new TuValue(l >= r);

                    case ETokenType.EqualEqual:     return new TuValue(l == r);
                    case ETokenType.NotEqual:       return new TuValue(r != l);
                    case ETokenType.Or:             return new TuValue(r != 0 || l != 0);
                    case ETokenType.And:            return new TuValue(r != 0 && l != 0);
                } 
            }

            if (left.type == EDataType.String || right.type == EDataType.String)
            {
                switch (op)
                {
                    case ETokenType.Plus: return new TuValue(left.AsString() + right.AsString());
                }
            }

            throw new Exception();
        }

        public TuValue Visit(NodeGroup n)
        {
            return n.Expression.Accept(this);
        }

        public TuValue Visit(NodeBlock n)
        {
            TuValue value = TuValue.Nothing;
            foreach (INode exp in n.Nodes)
            {
                value = exp.Accept(this);
            }
            return value;
        }

        public TuValue Visit(NodeIfElse n)
        {
            TuValue ifValue = n.If.Accept(this);
            if (ifValue.AsBoolean())
            {
                return n.IfBlock.Accept(this);
            }

            foreach ((INode elif, NodeBlock elifBlock) in n.ElseIfs)
            {
                TuValue elifValue = elifBlock.Accept(this);
                if (elifValue.AsBoolean())
                {
                    return elifBlock.Accept(this);
                }
            }

            if (n.ElseBlock is not null)
            {
                return n.ElseBlock.Accept(this);
            }
            return TuValue.Nothing;
        }

        public TuValue Visit(NodeDeclarationOrAssignment n)
        {
            TuValue value = n.Expression is not null ? n.Expression.Accept(this) : TuValue.Nothing;
            Variable variable = _stack.Current.GetOrDeclare(n.Token);

            if (variable.Value.type == value.type)
            {
                variable.Value = value;
            }
            else
            {
                throw new Exception();
            }
            return variable.Value;
        }

        public TuValue Visit(NodeRangeLiteral n)
        {
            TuValue from = n.From.Accept(this);
            if (from.type != EDataType.Numeric)
            {
                throw new Exception();
            }

            TuValue to = n.To.Accept(this);
            if (from.type != EDataType.Numeric)
            {
                throw new Exception();
            }

            TuValue by = n.By is null ? new TuValue(1) : n.By.Accept(this);
            if (from.type != EDataType.Numeric)
            {
                throw new Exception();
            }

            double f = from.AsNumeric();
            double t = to.AsNumeric();
            double b = by.AsNumeric();

            if ((t - f) / b < 0)
            {
                throw new Exception();
            }
            return new TuValue(new Range(f, t, b));
        }

        public TuValue Visit(NodeFor n)
        {
            Variable i = _stack.Current.GetOrDeclare(n.Identifier.Token);
            TuValue range = n.Range.Accept(this);

            TuValue inter = TuValue.Nothing;
            foreach (double item in range.AsRange())
            {
                i.Value = new TuValue(item);
                inter = n.Block.Accept(this);
            }
            return inter;
        }

        public TuValue Visit(NodeFun n)
        {
            string function = n.Token.value;

            TuValue[] args = new TuValue[n.Args.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = n.Args[i].Accept(this);
            }

            switch (function)
            {
                case "Print":
                    if (args.Length == 0)
                    {
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine(args[0].AsString());
                    }
                    return TuValue.Nothing;

                default:
                    throw new Exception($"Unknown function.");
            }

            throw new Exception();
        }

        public TuValue Visit(NodeTupleLiteral n)
        {
            List<double> values = new List<double>(n.Args.Count);
            foreach (INode item in n.Args)
            {
                TuValue v = item.Accept(this);
                if (v.type != EDataType.Numeric)
                {
                    throw new Exception();
                }
                values.Add(v.AsNumeric());
            }
            return new TuValue(new TuTuple(values));
        }
    }
}
