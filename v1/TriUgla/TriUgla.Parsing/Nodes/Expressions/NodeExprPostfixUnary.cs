using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprPostfixUnary : NodeExprBase
    {
        public NodeExprPostfixUnary(Token op, NodeExprBase exp) : base(op)
        {
            Expression = exp;
        }

        public Token Operation => Token;
        public NodeExprBase Expression { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            ETokenType op = Operation.type;
            if (op != ETokenType.PlusPlus && op != ETokenType.MinusMinus)
            {
                throw new CompileTimeException(
                    $"Unsupported postfix operation '{Operation.value}'.",
                    Operation);
            }

            if (Expression is not NodeExprIdentifier id)
            {
                throw new CompileTimeException(
                    $"Postfix '{Operation.value}' requires an identifier.",
                    Operation);
            }

            id.DeclareIfMissing = false;
            TuValue value = id.Evaluate(rt);
            Variable v = id.Variable!;
            if (!value.type.IsNumeric())
            {
                throw new CompileTimeException(
                    $"Postfix '{Operation.value}' requires a numeric variable, but '{v.Name}' has type '{value.type}'.",
                    id.Token);
            }

            double old = value.AsNumeric();
            double next = op == ETokenType.PlusPlus ? old + 1 : old - 1;

            TuValue toAssign, toReturn;
            if (value.type == EDataType.Integer)
            {
                toAssign = new TuValue((int)next);
                toReturn = new TuValue((int)old);
            }
            else
            {
                toAssign = new TuValue(next);
                toReturn = new TuValue(old);
            }

            v.Assign(toAssign);
            return toReturn;

        }
    }
}
