using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Nodes.TupleOps;

namespace TriUgla.Parsing
{
    public interface INodeEvaluationVisitor
    {
        TuValue Visit(ProgramNode n);

        TuValue Visit(NodeNumeric n);
        TuValue Visit(NodeString n);
        TuValue Visit(Nodes.Literals.NodeIdentifier n);
        TuValue Visit(NodeRange n);

        TuValue Visit(NodePrefixUnary n);
        TuValue Visit(NodePostfixUnary n);
        TuValue Visit(NodeBinary n);
        TuValue Visit(NodeGroup n);
        TuValue Visit(NodeBlock n);
        TuValue Visit(NodeIfElse n);
        TuValue Visit(Nodes.NodeIdentifier n);
        TuValue Visit(NodeFor n);
        TuValue Visit(NodeFunctionCall n);
        TuValue Visit(NodeTuple n);
        TuValue Visit(NodeLengthOf n);
        TuValue Visit(NodeValueAt n);
        TuValue Visit(NodeTernary n);

        TuValue Visit(NodeMacro n);
        TuValue Visit(NodeMacroCall n);
    }
}
