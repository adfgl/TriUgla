using System.Runtime.CompilerServices;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Evaluator : INodeVisitor
    {
        Stack _stack = new Stack();

        public Value Visit(NodeNumericLiteral n)
        {
            Value value = new Value(n.Token.numeric);
            return value;
        }

        public Value Visit(NodeStringLiteral n)
        {
            Value value = new Value(n.Token.value);
            return value;
        }

        public Value Visit(NodeIdentifierLiteral n)
        {
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

     
    }
}
