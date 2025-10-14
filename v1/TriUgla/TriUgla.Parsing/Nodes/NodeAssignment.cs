using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeIdentifier : INode
    {
        public NodeIdentifier(Token op, INode id, INode? expression)
        {
            Token = op;
            Assignee = id;
            Expression = expression;
        }

        public Token Token { get; }
        public INode Assignee { get; }
        public INode? Expression { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);
    }
}
