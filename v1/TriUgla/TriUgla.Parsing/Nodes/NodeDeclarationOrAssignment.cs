using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeDeclarationOrAssignment : INode
    {
        public NodeDeclarationOrAssignment(Token identifier, INode? expression)
        {
            Identifier = identifier;
            Expression = expression;
        }

        public Token Identifier { get; }
        public INode? Expression { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
