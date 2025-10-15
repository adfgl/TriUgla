using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeContinue : NodeBase
    {
        public NodeContinue(Token token) : base(token)
        {
        }

        public override TuValue Evaluate(TuRuntime stack)
        {
            stack.Flow.SignalContinue();
            return TuValue.Nothing;
        }
    }
}
