using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtFor : NodeBase
    {
        public NodeStmtFor(Token token, IEnumerable<NodeBase> args, NodeStmtBlock block) : base(token)
        {
            Block = block;
            Args = args.ToArray();
        }

        public IReadOnlyList<NodeBase> Args { get; }
        public NodeStmtBlock Block { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            if (Args.Count != 2 && Args.Count != 3)
            {
                throw new Exception();
            }

            NodeBase fromNode = Args[0], toNode = Args[1];
            NodeBase? stepNode = Args.Count == 3 ? Args[2] : null;

            double from = fromNode.Evaluate(stack).AsNumeric();
            double to = toNode.Evaluate(stack).AsNumeric();
            double step;
            if (stepNode != null)
            {
                step = stepNode.Evaluate(stack).AsNumeric();
            }
            else
            {
                step = (to >= from) ? 1.0 : -1.0;
            }

            var flow = stack.Flow;
            const double eps = 1e-12;
            if (step > 0)
            {
                for (double i = from; i <= to + eps; i += step)
                {
                    if (flow.HasReturn) break;

                    // counter.Value = new TuValue(i);

                    Block.Evaluate(stack);

                    if (flow.IsContinue) { flow.ConsumeBreakOrContinue(); continue; }
                    if (flow.IsBreak) { flow.ConsumeBreakOrContinue(); break; }
                }
            }
            else
            {
                for (double i = from; i >= to - eps; i += step)
                {
                    if (flow.HasReturn) break;

                    // counter.Value = new TuValue(i);

                    Block.Evaluate(stack);

                    if (flow.IsContinue) { flow.ConsumeBreakOrContinue(); continue; }
                    if (flow.IsBreak) { flow.ConsumeBreakOrContinue(); break; }
                }
            }

            flow.LeaveLoop();
            return TuValue.Nothing;
        }
    }
}
