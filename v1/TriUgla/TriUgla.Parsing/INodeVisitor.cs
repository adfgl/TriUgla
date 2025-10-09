using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Functions;
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
        Value Visit(NodeFunRad n);
        Value Visit(NodeFunDeg n);
        Value Visit(NodeFunTanh n);
        Value Visit(NodeFunSinh n);
        Value Visit(NodeFunCosh n);
        Value Visit(NodeFunFloor n);
        Value Visit(NodeFunCeil n);
        Value Visit(NodeFunRound n);
        Value Visit(NodeFunSign n);
        Value Visit(NodeFunMin n);
        Value Visit(NodeFunMax n);
    }
}
