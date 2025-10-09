using TriUgla.Parsing.Compiling;

namespace TriUgla.Parsing.Nodes
{
    public class NodeBlock : INode
    {
        public NodeBlock(IEnumerable<INode> statements)
        {
            Nodes = statements.ToArray();
        }

        public IReadOnlyList<INode> Nodes { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
