using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Functions
{
    public class NodeFunSign : NodeFun
    {
        public NodeFunSign(Token name, IEnumerable<INode> args) : base(name, args)
        {
        }

        public override Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
