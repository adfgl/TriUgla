using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;

namespace TriUgla.Parsing.Nodes
{
    public class NodeBlock : INode
    {
        public NodeBlock(IEnumerable<INode> statements)
        {
            Nodes = statements.ToArray();
        }

        public IReadOnlyList<INode> Nodes { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
