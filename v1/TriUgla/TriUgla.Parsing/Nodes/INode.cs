using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public interface INode
    {
        Token Token { get; }
        TuValue Accept(INodeVisitor visitor);
    }
}
