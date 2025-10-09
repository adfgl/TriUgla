using System.Runtime.CompilerServices;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;
using Range = TriUgla.Parsing.Compiling.Range;

namespace TriUgla.Parsing
{
    public class Evaluator : INodeVisitor
    {
        Stack _stack = new Stack();

        public Value Visit(NodeNumericLiteral n)
        {
            if (Double.TryParse(n.Token.value, out double d))
            {
                return new Value(d);
            }

            throw new Exception();
        }

        public Value Visit(NodeStringLiteral n)
        {
            Value value = new Value(n.Token.value);
            return value;
        }

        public static bool ValidIdentifier(string id)
        {
            return true;
        }

        public Value Visit(NodeIdentifierLiteral n)
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

        public Value Visit(NodeUnary n)
        {
            Value value = n.Expression.Accept(this);
            ETokenType op = n.Operation.type;
            if (value.type == EDataType.Numeric)
            {
                switch (op)
                {
                    case ETokenType.Minus: return new Value(-value.AsNumeric());
                    case ETokenType.Plus:  return value; 
                }
            }

            if (op == ETokenType.Not)
            {
                return new Value(!value.AsBoolean());
            }
            throw new Exception();
        }

        public Value Visit(NodeBinary n)
        {
            Value left = n.Left.Accept(this);
            Value right = n.Right.Accept(this);

            ETokenType op = n.Operation.type;

            if (left.type == EDataType.Numeric && right.type == EDataType.Numeric)
            {
                double l = left.AsNumeric();
                double r = right.AsNumeric();

                switch (op)
                {
                    case ETokenType.Minus:          return new Value(l - r);
                    case ETokenType.Plus:           return new Value(l + r);
                    case ETokenType.Slash:          return new Value(l / r);
                    case ETokenType.Star:           return new Value(l * r);
                    case ETokenType.Modulo:         return new Value(l % r);
                    case ETokenType.Power:          return new Value(Math.Pow(l, r));
                    case ETokenType.Less:           return new Value(l < r);
                    case ETokenType.Greater:        return new Value(l > r);
                    case ETokenType.LessOrEqual:    return new Value(l <= r);
                    case ETokenType.GreaterOrEqual: return new Value(l >= r);

                    case ETokenType.EqualEqual:     return new Value(l == r);
                    case ETokenType.NotEqual:       return new Value(r != l);
                    case ETokenType.Or:             return new Value(r != 0 || l != 0);
                    case ETokenType.And:            return new Value(r != 0 && l != 0);
                } 
            }

            if (left.type == EDataType.String || right.type == EDataType.String)
            {
                switch (op)
                {
                    case ETokenType.Plus: return new Value(left.AsString() + right.AsString());
                }
            }

            throw new Exception();
        }

        public Value Visit(NodeGroup n)
        {
            return n.Expression.Accept(this);
        }

        public Value Visit(NodeBlock n)
        {
            Value value = Value.Nothing;
            foreach (INode exp in n.Nodes)
            {
                value = exp.Accept(this);
            }
            return value;
        }

        public Value Visit(NodeIfElse n)
        {
            Value ifValue = n.If.Accept(this);
            if (ifValue.AsBoolean())
            {
                return n.IfBlock.Accept(this);
            }

            foreach ((INode elif, NodeBlock elifBlock) in n.ElseIfs)
            {
                Value elifValue = elifBlock.Accept(this);
                if (elifValue.AsBoolean())
                {
                    return elifBlock.Accept(this);
                }
            }

            if (n.ElseBlock is not null)
            {
                return n.ElseBlock.Accept(this);
            }
            return Value.Nothing;
        }

        public Value Visit(NodeDeclarationOrAssignment n)
        {
            Value value = n.Expression is not null ? n.Expression.Accept(this) : Value.Nothing;
            Variable variable = _stack.Current.Declare(n.Identifier, value);
            return variable.Value;
        }

        public Value Visit(NodeRangeLiteral n)
        {
            Value from = n.From.Accept(this);
            if (from.type != EDataType.Numeric)
            {
                throw new Exception();
            }

            Value to = n.To.Accept(this);
            if (from.type == EDataType.Numeric)
            {
                throw new Exception();
            }

            Value by = n.By is null ? new Value(1) : n.By.Accept(this);
            if (from.type == EDataType.Numeric)
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
            return new Value(new Range(f, t, b));
        }

        public Value Visit(NodeFor n)
        {
            throw new NotImplementedException();
        }

        public Value Visit(NodeFun n)
        {
            throw new NotImplementedException();
        }
    }
}
