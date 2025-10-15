using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtAbort : NodeStmtBase
    {
        public NodeStmtAbort(Token token) : base(token)
        {
        }

        protected override TuValue Evaluate(TuRuntime stack)
        {
            stack.Budget.FullStop();
            return TuValue.Nothing;
        }
    }
}
