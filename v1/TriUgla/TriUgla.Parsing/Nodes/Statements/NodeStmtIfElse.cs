using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtIfElse : NodeStmtBase
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

        protected override TuValue Eval(TuRuntime stack)
        {
            var flow = stack.Flow;

            foreach (var (cond, block) in Branches)
            {
                if (!stack.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue) break;

                if (cond.Evaluate(stack).AsBoolean())
                {
                    block.Evaluate(stack);
                    return TuValue.Nothing; 
                }
            }

            if (ElseBlock is null || !stack.Budget.Tick() || flow.HasReturn || flow.IsBreak || flow.IsContinue)
            {
                return TuValue.Nothing;
            }

            ElseBlock.Evaluate(stack);
            return TuValue.Nothing;
        }

    }
}
