using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Runtime;
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
            if (Args.Count is not (2 or 3))
                throw new Exception("for expects 2 or 3 arguments: from, to[, by].");

            var (from, loopVar) = BindFrom(stack, Args[0]);
            NodeBase toNode = Args[1];
            NodeBase? byNode = Args.Count == 3 ? Args[2] : null;

            var flow = stack.Flow;
            double i = from;

            for (; ; )
            {
                if (!PreIteration(stack, flow)) break;

                double to = ReadTo(stack, toNode, from);
                double by = ReadBy(stack, byNode, from, to);

                if (!ShouldContinueExclusive(i, to, by)) break;

                AssignLoopVar(loopVar, i);

                Block.Evaluate(stack);

                if (!PostIteration(stack, flow, ref i, by)) break;
            }

            flow.LeaveLoop();
            return TuValue.Nothing;
        }

        static (double start, Variable? loopVar) BindFrom(TuRuntime st, NodeBase node)
        {
            if (node is NodeExprIdentifier id)
            {
                id.DeclareIfMissing = true;
                TuValue cur = id.Evaluate(st);
                double v = cur.type switch
                {
                    EDataType.Nothing => 0.0,
                    EDataType.Numeric => cur.AsNumeric(),
                    _ => throw new Exception("'from' must be numeric.")
                };
                return (v, st.Current.Get(id.Name));
            }

            if (node is NodeExprAssignment a && a.Assignee is NodeExprIdentifier tgt)
            {
                TuValue cur = a.Evaluate(st);
                if (cur.type != EDataType.Numeric) throw new Exception("'from' assignment must be numeric.");
                return (cur.AsNumeric(), st.Current.Get(tgt.Name));
            }

            return (EvalNumOrDefault(st, node, "from", 0.0), null);
        }

        static double ReadTo(TuRuntime st, NodeBase toNode, double @from)
            => EvalNumOrDefault(st, toNode, "to", @from);

        static double ReadBy(TuRuntime st, NodeBase? byNode, double from, double to)
        {
            double def = (to >= from) ? 1.0 : -1.0;
            double by = EvalNumOrDefault(st, byNode, "by", def);
            if (by == 0.0) throw new Exception("'by' (step) cannot be zero.");
            return by;
        }

        static double EvalNumOrDefault(TuRuntime st, NodeBase? node, string name, double def)
        {
            if (node is null) return def;
            TuValue v = node.Evaluate(st);
            if (v.type != EDataType.Numeric) throw new Exception($"'{name}' must be numeric.");
            return v.AsNumeric();
        }

        static bool ShouldContinueExclusive(double i, double to, double by)
        {
            int dir = Math.Sign(by);
            return dir > 0 ? (i < to)
                 : dir < 0 ? (i > to)
                 : throw new Exception("'by' (step) cannot be zero."); // double safety
        }

        static void AssignLoopVar(Variable? loopVar, double i)
        {
            if (loopVar is not null) loopVar.Value = new TuValue(i);
        }

        static bool PreIteration(TuRuntime st, RuntimeFlow flow)
        {
            if (!st.Budget.Tick()) return false;
            if (flow.HasReturn) return false;
            return true;
        }

        static bool PostIteration(TuRuntime st, RuntimeFlow flow, ref double i, double by)
        {
            if (!st.Budget.Tick()) return false;

            if (flow.IsContinue)
            {
                flow.ConsumeBreakOrContinue();
                i += by;
                return true;
            }

            if (flow.IsBreak)
            {
                flow.ConsumeBreakOrContinue();
                return false;
            }
            i += by;
            return true;
        }

    }
}
