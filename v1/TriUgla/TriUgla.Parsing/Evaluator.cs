using System.Runtime.CompilerServices;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Functions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

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
            if (value.type == EDataType.Numeric)
            {
                switch (n.Operation.type)
                {
                    case ETokenType.Not:   return new Value(!(value.numeric != 0));
                    case ETokenType.Minus: return new Value(-value.numeric);
                    case ETokenType.Plus:  return value; 
                }
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
                switch (op)
                {
                    case ETokenType.Minus:          return new Value(left.numeric - right.numeric);
                    case ETokenType.Plus:           return new Value(left.numeric + right.numeric);
                    case ETokenType.Slash:          return new Value(left.numeric / right.numeric);
                    case ETokenType.Star:           return new Value(left.numeric * right.numeric);
                    case ETokenType.Modulo:         return new Value(left.numeric % right.numeric);
                    case ETokenType.Power:          return new Value(Math.Pow(left.numeric, right.numeric));
                    case ETokenType.Less:           return new Value(left.numeric < right.numeric);
                    case ETokenType.Greater:        return new Value(left.numeric > right.numeric);
                    case ETokenType.LessOrEqual:    return new Value(left.numeric <= right.numeric);
                    case ETokenType.GreaterOrEqual: return new Value(left.numeric >= right.numeric);
                    case ETokenType.EqualEqual:     return new Value(left.numeric == right.numeric);
                    case ETokenType.NotEqual:       return new Value(right.numeric != left.numeric);
                    case ETokenType.Or:             return new Value(right.numeric != 0 || left.numeric != 0);
                    case ETokenType.And:            return new Value(right.numeric != 0 && left.numeric != 0);
                } 
            }

            if (left.type == EDataType.String || right.type == EDataType.String)
            {
                switch (op)
                {
                    case ETokenType.Plus: return new Value(Value.AsText(in left) + Value.AsText(in right));
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
            if (Value.Truthy(in ifValue))
            {
                return n.IfBlock.Accept(this);
            }

            foreach ((INode elif, NodeBlock elifBlock) in n.ElseIfs)
            {
                Value elifValue = elifBlock.Accept(this);
                if (Value.Truthy(in elifValue))
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

        Value SingleArgFunction(NodeFun fn, Func<double, double> f)
        {
            ValidateNumberOfArguments(1, 1, fn.Args.Count);
            Value v = fn.Args[0].Accept(this);
            if (v.type == EDataType.Numeric)
            {
                return new Value(f(v.numeric));
            }
            throw new Exception();
        }

        void ValidateNumberOfArguments(int min, int max, int args)
        {
            if (min <= args && args <= max)
            {
                return;
            }
        }

        public Value Visit(NodeFunAbs n) => SingleArgFunction(n, Math.Abs);
        public Value Visit(NodeFunSqrt n) => SingleArgFunction(n, Math.Sqrt);
        public Value Visit(NodeFunExp n) => SingleArgFunction(n, Math.Exp);
        public Value Visit(NodeFunSin n) => SingleArgFunction(n, Math.Sin);
        public Value Visit(NodeFunCos n) => SingleArgFunction(n, Math.Cos);
        public Value Visit(NodeFunLog n) => SingleArgFunction(n, Math.Log);
        public Value Visit(NodeFunLog10 n) => SingleArgFunction(n, Math.Log10);
        public Value Visit(NodeFunAtan n) => SingleArgFunction(n, Math.Atan);
        public Value Visit(NodeFunAcos n) => SingleArgFunction(n, Math.Acos);
        public Value Visit(NodeFunAsin n) => SingleArgFunction(n, Math.Asin);
        public Value Visit(NodeFunTan n) => SingleArgFunction(n, Math.Tan);
    }
}
