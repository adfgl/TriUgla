using TriUgla.Parsing.Compiling;
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

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"'{Token.value}'";
        }
    }
}
