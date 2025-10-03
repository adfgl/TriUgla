using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeIdentifierLiteral : INode
    {
        public NodeIdentifierLiteral(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
