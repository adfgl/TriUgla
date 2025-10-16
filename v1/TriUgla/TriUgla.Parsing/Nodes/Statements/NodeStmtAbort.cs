using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public sealed class NodeStmtAbort : NodeStmtBase
    {
        public NodeStmtAbort(Token token) : base(token)
        {
        }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            rt.Budget.FullStop();
            return TuValue.Nothing;
        }
    }
}
