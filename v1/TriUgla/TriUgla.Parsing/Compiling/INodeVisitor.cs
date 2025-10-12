using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Nodes.TupleOps;

namespace TriUgla.Parsing.Compiling
{
    public interface INodeVisitor
    {
        TuValue Visit(NodeNumeric n);
        TuValue Visit(NodeString n);
        TuValue Visit(NodeIdentifier n);
        TuValue Visit(NodeRange n);

        TuValue Visit(NodePrefixUnary n);
        TuValue Visit(NodePostfixUnary n);
        TuValue Visit(NodeBinary n);
        TuValue Visit(NodeGroup n);
        TuValue Visit(NodeBlock n);
        TuValue Visit(NodeIfElse n);
        TuValue Visit(NodeAssignment n);
        TuValue Visit(NodeFor n);
        TuValue Visit(NodeFunctionCall n);
        TuValue Visit(NodeTuple n);
        TuValue Visit(NodeLengthOf n);
        TuValue Visit(NodeValueAt n);
    }
}
