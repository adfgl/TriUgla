using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public sealed class NodeStmtForIn : NodeStmtBase
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

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            NodeExprIdentifier? id = Counter as NodeExprIdentifier;
            if (id is null)
            {
                throw new CompileTimeException($"Expected identifier but got '{Counter.Token.type}'.", Counter.Token);
            }

            id.DeclareIfMissing = true;
            id.Evaluate(rt);
            Variable counter = id.Variable!;

            TuValue list = Range.Evaluate(rt);
            IEnumerable<TuValue> iterator = list.type switch
            {
                EDataType.Range => list.AsRange()!,
                EDataType.List => list.AsTuple()!,
                _ => throw new RunTimeException($"For-loop expects {EDataType.Range} or {EDataType.List} but got {list.type}.", Range.Token),
            };

            var flow = rt.Flow;
            flow.EnterLoop();

            foreach (TuValue item in iterator)
            {
                if (!rt.Budget.Tick() || flow.HasReturn) break;

                counter.Assign(item);

                Block.Evaluate(rt);

                if (!rt.Budget.Tick()) break;

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
