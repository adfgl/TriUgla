using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public interface INode
    {
        Value Accept(INodeVisitor visitor);
    }
}
