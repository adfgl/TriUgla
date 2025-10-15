using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprTernary : NodeExprBase
    {
        public NodeExprTernary(NodeBase ifExp, Token question, NodeBase thenExp, Token colon, NodeBase elseExp) : base(question)
        {
            IfExp = ifExp;
            ThenExp = thenExp;
            Colon = colon;
            ElseExp = elseExp;
        }

        public NodeBase IfExp { get; }
        public Token Question => Token;
        public NodeBase ThenExp { get; }
        public Token Colon { get; }
        public NodeBase ElseExp { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            TuValue result;

            TuValue ifValue = IfExp.Evaluate(stack);
            CheckResult(ifValue, IfExp);

            bool condition = ifValue.AsBoolean();
            if (ifValue.AsBoolean())
            {
                result = ThenExp.Evaluate(stack);
            }
            else
            {
                result = ElseExp.Evaluate(stack);
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
