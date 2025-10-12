using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeTuple : INode
    {
        public NodeTuple(Token token, IEnumerable<INode> args)
        {
            Token = token;
            Args = args.ToArray();
        }

        public Token Token { get; }
        public IReadOnlyList<INode> Args { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);

    }
}
