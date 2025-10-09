using TriUgla.Parsing.Compiling;

namespace TriUgla.Parsing.Nodes
{
    public interface INode
    {
        Value Accept(INodeVisitor visitor);
    }
}
