using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeGroup : INode
    {
        public NodeGroup(Token token, INode exp)
        {
            Expression = exp;
        }

        public Token Token { get; }
        public INode Expression { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}
