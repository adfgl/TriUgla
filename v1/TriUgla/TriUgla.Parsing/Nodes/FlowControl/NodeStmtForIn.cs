using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtForIn : NodeBase
    {
        public NodeStmtForIn(Token start, NodeBase id, NodeBase range, NodeStmtBlock block, Token end) : base(start)
        {
            Counter = id;
            Range = range;
            Block = block;
            End = end;
        }

        public Token Start => Token;
        public Token End { get; }

        public NodeBase Counter { get; }
        public NodeBase Range { get; }
        public NodeStmtBlock Block { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            NodeExprIdentifier? id = Counter as NodeExprIdentifier;
            if (id is null)
            {
                throw new CompileTimeException($"Expected identifier but got '{Counter.Token.type}'.", Counter.Token);
            }

            id.DeclareIfMissing = true;
            id.Evaluate(stack);
            Variable counter = stack.Current.Get(id.Name)!;

            TuValue list = Range.Evaluate(stack);
            IEnumerable<double> iterator = list.type switch
            {
                EDataType.Range => list.AsRange()!,
                EDataType.Tuple => list.AsTuple()!,
                _ => throw new RunTimeException($"For-loop expects {EDataType.Range} or {EDataType.Tuple} but got {list.type}.", Range.Token),
            };

            var flow = stack.Flow;
            flow.EnterLoop();

            foreach (var item in iterator)
            {
                if (!stack.Budget.Tick() || flow.HasReturn) break;

                counter.Assign(new TuValue(item));

                Block.Evaluate(stack);

                if (!stack.Budget.Tick()) break;

                if (flow.IsContinue)
                {
                    flow.ConsumeBreakOrContinue();
                    continue;
                }
                if (flow.IsBreak)
                {
                    flow.ConsumeBreakOrContinue();
                    break;
                }
            }

            flow.LeaveLoop();

            return TuValue.Nothing;
        }
    }
}
