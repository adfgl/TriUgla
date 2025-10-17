using TriScript.Data;

namespace TriScript.Parsing.Nodes
{
    public abstract class NodeBase
    {
        public abstract Value Evaluate(Executor rt);
    }
}
