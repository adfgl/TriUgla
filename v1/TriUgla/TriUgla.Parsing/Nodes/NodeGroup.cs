
using TriUgla.Parsing.Compiling;

namespace TriUgla.Parsing.Nodes
{
    public class NodeGroup : INode
    {
        public NodeGroup(INode exp)
        {
            Expression = exp;
        }

        public INode Expression { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
