using TriUgla.Parsing.Compiling;

namespace TriUgla.Parsing.Nodes
{
    public interface INode
    {
        TuValue Accept(INodeVisitor visitor);
    }
}
