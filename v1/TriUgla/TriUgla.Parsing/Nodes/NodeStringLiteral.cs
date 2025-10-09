using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeStringLiteral : INode
    {
        public NodeStringLiteral(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return Token.value;
        }
    }
}
