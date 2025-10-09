using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeUnary : INode
    {
        public NodeUnary(Token op, INode exp)
        {
            Operation = op;
            Expression = exp;
        }
        
        public Token Operation { get; }
        public INode Expression { get; }

        public Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
