using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeBinary : INode
    {
        public NodeBinary(INode left, Token op, INode right)
        {
            Left = left;
            Token = op;
            Right = right;
        }

        public INode Left { get; }
        public Token Token { get; }
        public INode Right { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
