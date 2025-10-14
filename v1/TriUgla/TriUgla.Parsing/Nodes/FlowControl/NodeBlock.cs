using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeBlock : NodeBase
    {
        public NodeBlock(Token token, IEnumerable<NodeBase> statements) : base(token)
        {
            Statements = statements.ToArray();
        }

        public IReadOnlyList<NodeBase> Statements { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            foreach (NodeBase node in Statements)
            {
                node.Evaluate(stack);
            }
            return TuValue.Nothing;
        }
    }
}
