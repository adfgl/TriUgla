using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeBlock : INode
    {
        public NodeBlock(Token token, IEnumerable<INode> statements)
        {
            Nodes = statements.ToArray();
        }

        public Token Token { get; }
        public IReadOnlyList<INode> Nodes { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);

    }
}
