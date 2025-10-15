using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprTernary : NodeExprBase
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

        protected override TuValue Evaluate(TuRuntime stack)
        {
            TuValue result;

            TuValue ifValue = IfExp.Eval(stack);
            CheckResult(ifValue, IfExp);

            bool condition = ifValue.AsBoolean();
            if (ifValue.AsBoolean())
            {
                result = ThenExp.Eval(stack);
            }
            else
            {
                result = ElseExp.Eval(stack);
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
