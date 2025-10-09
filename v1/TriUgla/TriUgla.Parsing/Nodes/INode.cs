using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;

namespace TriUgla.Parsing.Nodes
{
    public interface INode
    {
        TuValue Accept(INodeVisitor visitor);
    }
}
