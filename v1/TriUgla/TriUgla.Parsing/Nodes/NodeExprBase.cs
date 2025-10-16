using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeExprBase : NodeBase
    {
        protected NodeExprBase(Token token) : base(token)
        {
        }

        public override TuValue Evaluate(TuRuntime rt)
        {
            return EvaluateInvariant(rt);
        }
    }
}
