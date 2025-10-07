using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Functions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public interface INodeVisitor
    {
        Value Visit(NodeNumericLiteral n);
        Value Visit(NodeStringLiteral n);
        Value Visit(NodeIdentifierLiteral n);
        Value Visit(NodeUnary n);
        Value Visit(NodeBinary n);
        Value Visit(NodeGroup n);
        Value Visit(NodeBlock n);
        Value Visit(NodeIfElse n);
        Value Visit(NodeDeclarationOrAssignment n);

        Value Visit(NodeFunAbs n);
        Value Visit(NodeFunSqrt n);
        Value Visit(NodeFunExp n);
        Value Visit(NodeFunSin n);
        Value Visit(NodeFunCos n);
        Value Visit(NodeFunLog n);
        Value Visit(NodeFunLog10 n);
        Value Visit(NodeFunAtan n);
        Value Visit(NodeFunAcos n);
        Value Visit(NodeFunAsin n);
        Value Visit(NodeFunTan n);
    }
}
