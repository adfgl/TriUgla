using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprPostfixUnary : NodeExprBase
    {
        public NodeExprPostfixUnary(Token op, NodeExprBase exp) : base(op)
        {
            Expression = exp;
        }

        public Token Operation => Token;
        public NodeExprBase Expression { get; }

        protected override TuValue Eval(TuRuntime stack)
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

            TuValue value = id.Evaluate(stack);

            Variable? v = stack.Current.Get(id.Name);
            if (v is null)
            {
                throw new CompileTimeException(
                    $"Variable '{id.Name}' is not defined.",
                    id.Token);
            }

            TuValue curVal = v.Value;

            if (curVal.type == EDataType.Nothing)
            {
                throw new CompileTimeException(
                    $"Variable '{id.Name}' is uninitialized; cannot apply '{Operation.value}'.",
                    id.Token);
            }

            if (curVal.type != EDataType.Numeric)
            {
                throw new CompileTimeException(
                    $"Postfix '{Operation.value}' requires a numeric variable, but '{id.Name}' has type '{curVal.type}'.",
                    id.Token);
            }

            double old = curVal.AsNumeric();
            double next = op == ETokenType.PlusPlus ? old + 1 : old - 1;

            v.Assign(new TuValue(next));
            return new TuValue(old);

        }
    }
}
