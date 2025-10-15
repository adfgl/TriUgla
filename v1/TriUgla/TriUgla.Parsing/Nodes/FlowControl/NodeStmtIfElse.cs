using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtIfElse : NodeBase
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

        public override TuValue Evaluate(TuRuntime stack)
        {
            var flow = stack.Flow;

            foreach (var (cond, block) in Branches)
            {
                if (flow.HasReturn || flow.IsBreak || flow.IsContinue) break;

                if (cond.Evaluate(stack).AsBoolean())
                {
                    block.Evaluate(stack);
                    return TuValue.Nothing; // any flow set inside bubbles up
                }
            }

            if (!flow.HasReturn && !flow.IsBreak && !flow.IsContinue && ElseBlock is not null)
                ElseBlock.Evaluate(stack);

            return TuValue.Nothing;
        }
    }
}
