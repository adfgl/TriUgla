using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeIdentifier : INode
    {
        public NodeIdentifier(Token token, INode? id)
        {
            Token = token;
            Id = id;
        }

        public Token Token { get; }
        public INode? Id { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override string ToString() => Id is null
         ? $"'{Token.value}'"
         : $"'{Token.value}({Id})'";
    }
}
