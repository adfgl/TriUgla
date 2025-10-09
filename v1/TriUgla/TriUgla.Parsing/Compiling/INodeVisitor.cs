using TriUgla.Parsing.Nodes;

namespace TriUgla.Parsing.Compiling
{
    public interface INodeVisitor
    {
        TuValue Visit(NodeNumericLiteral n);
        TuValue Visit(NodeStringLiteral n);
        TuValue Visit(NodeIdentifierLiteral n);
        TuValue Visit(NodeRangeLiteral n);

        TuValue Visit(NodeUnary n);
        TuValue Visit(NodeBinary n);
        TuValue Visit(NodeGroup n);
        TuValue Visit(NodeBlock n);
        TuValue Visit(NodeIfElse n);
        TuValue Visit(NodeDeclarationOrAssignment n);
        TuValue Visit(NodeFor n);
        TuValue Visit(NodeFun n);
        TuValue Visit(NodeTupleLiteral n);
    }
}
