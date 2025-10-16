using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprPrefixUnary : NodeExprBase
    {
        public NodeExprPrefixUnary(Token op, NodeExprBase exp) : base(op)
        {
            Expression = exp;
        }

        public Token Operation => Token;
        public NodeExprBase Expression { get; }

        TuValue EvaluateIncrement(TuRuntime rt)
        {
            ETokenType op = Operation.type;
            if (op != ETokenType.PlusPlus && op != ETokenType.MinusMinus)
            {
                throw new Exception();
            }

            if (Expression is not NodeExprIdentifier id)
            {
                throw new CompileTimeException(
                    $"Prefix '{Operation.value}' requires an identifier.",
                    Operation);
            }

            id.DeclareIfMissing = false;
            TuValue value = id.Evaluate(rt);
            Variable v = id.Variable!;
            if (!value.type.IsNumeric())
            {
                throw new CompileTimeException(
                    $"Prefix '{Operation.value}' requires a numeric variable, but '{v.Name}' has type '{value.type}'.",
                    id.Token);
            }

            double old = value.AsNumeric();
            double next = op == ETokenType.PlusPlus ? old + 1 : old - 1;

            TuValue toAssign;
            if (value.type == EDataType.Integer)
            {
                toAssign = new TuValue((int)next);
            }
            else
            {
                toAssign = new TuValue(next);
            }

            v.Assign(toAssign);
            return v.Value;
        }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            ETokenType op = Operation.type;

            TuValue value;
            if (op == ETokenType.PlusPlus || op == ETokenType.MinusMinus)
            {
                return EvaluateIncrement(rt);
            }

            value = Expression.Evaluate(rt);
            if (op == ETokenType.Not)
            {
                return new TuValue(!value.AsBoolean());
            }

            if (op == ETokenType.Plus || op == ETokenType.Minus)
            {
                if (!value.type.IsNumeric())
                {
                    throw new RunTimeException(
               $"Prefix '{Token.value}' requires operand to evaluate to numeric, but got '{value.type}'.",
               Expression.Token);
                }
                return op == ETokenType.Plus ? value : -value;
            }

            throw new CompileTimeException($"Unsupported unary operation '{Operation.value}'.", Operation);
        }
    }
}
