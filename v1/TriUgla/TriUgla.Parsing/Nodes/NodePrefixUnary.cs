using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodePrefixUnary : INode
    {
        public NodePrefixUnary(Token op, INode exp)
        {
            Token = op;
            Expression = exp;
        }
        
        public Token Token { get; }
        public INode Expression { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
