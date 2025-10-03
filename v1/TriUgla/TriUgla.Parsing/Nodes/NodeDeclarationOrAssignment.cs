using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeDeclarationOrAssignment : INode
    {
        public NodeDeclarationOrAssignment(Token identifier, INode expression)
        {
            Identifier = identifier;
            Expression = expression;
        }

        public Token Identifier { get; }
        public INode? Expression { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
