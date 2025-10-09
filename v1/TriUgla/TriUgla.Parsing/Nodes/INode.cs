using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Compiling.RuntimeObjects;

namespace TriUgla.Parsing.Nodes
{
    public interface INode
    {
        Value Accept(INodeVisitor visitor);
    }
}
