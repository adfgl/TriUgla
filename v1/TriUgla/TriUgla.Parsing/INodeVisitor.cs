using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Literals;

namespace TriUgla.Parsing
{
    public interface INodeVisitor
    {
        Value Visit(NodeNumericLiteral n);
        Value Visit(NodeStringLiteral n);
        Value Visit(NodeIdentifierLiteral n);
        Value Visit(NodeRangeLiteral n);

        Value Visit(NodeUnary n);
        Value Visit(NodeBinary n);
        Value Visit(NodeGroup n);
        Value Visit(NodeBlock n);
        Value Visit(NodeIfElse n);
        Value Visit(NodeDeclarationOrAssignment n);
        Value Visit(NodeFor n);
        Value Visit(NodeFun n);
    }
}
