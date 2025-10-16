using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtForIn : NodeStmtBase
    {
        public NodeStmtForIn(Token start, NodeExprBase id, NodeExprBase range, NodeStmtBlock block, Token end) : base(start)
        {
            Counter = id;
            Range = range;
            Block = block;
            End = end;
        }

        public Token Start => Token;
        public Token End { get; }

        public NodeExprBase Counter { get; }
        public NodeExprBase Range { get; }
        public NodeStmtBlock Block { get; }

        protected override TuValue Eval(TuRuntime stack)
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
