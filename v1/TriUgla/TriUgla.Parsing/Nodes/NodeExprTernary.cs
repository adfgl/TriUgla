using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeExprTernary : NodeBase
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

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue result;

            TuValue ifValue = IfExp.Evaluate(stack);
            if (ifValue.AsBoolean())
            {
                result = ThenExp.Evaluate(stack);
            }
            else
            {
                result = ElseExp.Evaluate(stack);
            }
            return result;
        }
    }
}
