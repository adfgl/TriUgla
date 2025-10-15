using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtBlock : NodeBase
    {
        public NodeStmtBlock(Token token, IEnumerable<NodeBase> statements) : base(token)
        {
            Statements = statements.ToArray();
        }

        public IReadOnlyList<NodeBase> Statements { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue result = TuValue.Nothing;
            foreach (NodeBase node in Statements)
            {
                TuValue value = node.Evaluate(stack);
                if (value.type != EDataType.Nothing)
                {
                    result = value;
                }
            }
            return TuValue.Nothing;
        }
    }
}
