using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public sealed class NodeStmtIfElse : NodeStmtBase
    {
        public NodeStmtIfElse(Token start, IEnumerable<(NodeBase condition, NodeStmtBlock block)> ifBlocks, NodeStmtBlock? elseBlock, Token end) : base(start)
        {
            Branches = ifBlocks.ToArray();
            End = end;
        }

        public IReadOnlyList<(NodeBase condition, NodeStmtBlock block)> Branches { get; }
        public NodeStmtBlock? ElseBlock { get; }

        public Token Start => Token;
        public Token End { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            var flow = rt.Flow;

            foreach (var (cond, block) in Branches)
            {
                if (!rt.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue) break;

                if (cond.Evaluate(rt).AsBoolean())
                {
                    block.Evaluate(rt);
                    return TuValue.Nothing; 
                }
            }

            if (ElseBlock is null || !rt.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue)
            {
                return TuValue.Nothing;
            }

            ElseBlock.Evaluate(rt);
            return TuValue.Nothing;
        }

    }
}
