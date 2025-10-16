using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprTernary : NodeExprBase
    {
        public NodeExprTernary(NodeExprBase ifExp, Token question, NodeExprBase thenExp, Token colon, NodeExprBase elseExp) : base(question)
        {
            IfExp = ifExp;
            ThenExp = thenExp;
            Colon = colon;
            ElseExp = elseExp;
        }

        public NodeExprBase IfExp { get; }
        public Token Question => Token;
        public NodeExprBase ThenExp { get; }
        public Token Colon { get; }
        public NodeExprBase ElseExp { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            TuValue result;

            TuValue ifValue = IfExp.Evaluate(rt);
            CheckResult(ifValue, IfExp);

            bool condition = ifValue.AsBoolean();
            if (ifValue.AsBoolean())
            {
                result = ThenExp.Evaluate(rt);
            }
            else
            {
                result = ElseExp.Evaluate(rt);
            }

            CheckResult(result, condition ? ThenExp : ElseExp);
            return result;
        }

        static void CheckResult(TuValue value, NodeBase node)
        {
            if (value.type == EDataType.Nothing)
            {
                throw new RunTimeException(
             $"Ternary condition must evaluate to boolean/numeric, but got '{value.type}'.", node.Token);
            }
        }
    }
}
