using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeStmtBase : NodeBase
    {
        protected NodeStmtBase(Token token) : base(token)
        {
        }

        public override TuValue Evaluate(TuRuntime rt)
        {
            base.Evaluate(rt);
            return TuValue.Nothing;
        }
    }
}
