using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprPrefixUnary : NodeExprBase
    {
        public NodeExprPrefixUnary(Token op, NodeBase exp) : base(op)
        {
            Expression = exp;
        }

        public Token Operation => Token;
        public NodeBase Expression { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            ETokenType op = Operation.type;

            TuValue value;
            if (op == ETokenType.PlusPlus || op == ETokenType.MinusMinus)
            {
                if (Expression is not NodeExprIdentifier id)
                {
                    throw new CompileTimeException(
                        $"Prefix '{Operation.value}' requires an identifier.",
                        Operation);
                }

                value = id.Evaluate(stack);
                if (value.type != EDataType.Numeric)
                {
                    throw new Exception($"Postfix {Operation.value} requires numeric variable");
                }

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
                        $"Prefix '{Operation.value}' requires a numeric variable, but '{id.Name}' has type '{curVal.type}'.",
                        id.Token);
                }

                double cur = curVal.AsNumeric();
                double next = op == ETokenType.PlusPlus ? cur + 1 : cur - 1;
                v.Assign(new TuValue(next));
                return v.Value;
            }

            value = Expression.Evaluate(stack);

            if (value.type == EDataType.Nothing)
            {
                if (Expression is NodeExprIdentifier id2)
                {
                    throw new CompileTimeException(
                        $"Operand of prefix '{Operation.value}' cannot be 'Nothing': variable '{id2.Name}' is undefined or uninitialized.",
                        id2.Token);
                }

                throw new RunTimeException(
                    $"Operand of prefix '{Operation.value}' evaluated to 'Nothing'.",
                    Expression.Token);
            }

            if (op == ETokenType.Not)
            {
                if (value.type != EDataType.Numeric)
                {
                    if (Expression is NodeExprIdentifier id3)
                    {
                        throw new CompileTimeException(
                            $"Prefix '{Token.value}' requires numeric/boolean (0 = false, nonzero = true): variable '{id3.Name}' has type '{value.type}'.",
                            id3.Token);
                    }

                    throw new RunTimeException(
                        $"Prefix '{Token.value}' requires operand to evaluate to numeric/boolean, but got '{value.type}'.",
                        Expression.Token);
                }

                return new TuValue(!value.AsBoolean());
            }

            if (op == ETokenType.Plus)
            {
                if (value.type != EDataType.Numeric)
                {
                    ThrowNonNumericUnary("+", value, Expression);
                }
                return value;
            }

            if (op == ETokenType.Minus)
            {
                if (value.type != EDataType.Numeric)
                {
                    ThrowNonNumericUnary("-", value, Expression);
                }
                return new TuValue(-value.AsNumeric());
            }

            throw new CompileTimeException(
            $"Unsupported unary operation '{Operation.value}'.", Operation);
        }

        static void ThrowNonNumericUnary(string opLexeme, in TuValue v, NodeBase expr)
        {
            if (expr is NodeExprIdentifier id)
            {
                throw new CompileTimeException(
                    $"Prefix '{opLexeme}' requires a numeric operand: variable '{id.Name}' has type '{v.type}'.",
                    id.Token);
            }

            throw new RunTimeException(
                $"Prefix '{opLexeme}' requires operand to evaluate to numeric, but got '{v.type}'.",
                expr.Token);
        }
    }
}
