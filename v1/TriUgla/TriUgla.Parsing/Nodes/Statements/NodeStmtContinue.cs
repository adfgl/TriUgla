using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtContinue : NodeStmtBase
    {
        public NodeStmtContinue(Token token) : base(token)
        {
        }

        protected override TuValue Eval(TuRuntime stack)
        {
            stack.Flow.SignalContinue();
            return TuValue.Nothing;
        }
    }
}
