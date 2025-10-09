using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeDeclarationOrAssignment : INode
    {
        public NodeDeclarationOrAssignment(Token identifier, INode? expression)
        {
            Token = identifier;
            Expression = expression;
        }

        public Token Token { get; }
        public INode? Expression { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
