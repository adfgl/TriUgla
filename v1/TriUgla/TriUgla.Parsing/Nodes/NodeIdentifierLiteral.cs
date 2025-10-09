using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeIdentifierLiteral : INode
    {
        public NodeIdentifierLiteral(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"'{Token.value}'";
        }
    }
}
