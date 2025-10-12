using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeRange : INode
    {
        public NodeRange(Token token, INode from, INode to, INode? by)
        {
            Token = token;  
            From = from;
            To = to;
            By = by;
        }

        public Token Token { get; }

        public INode From { get; }
        public INode To { get; }
        public INode? By { get; }

        public TuValue Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }

    }
}
