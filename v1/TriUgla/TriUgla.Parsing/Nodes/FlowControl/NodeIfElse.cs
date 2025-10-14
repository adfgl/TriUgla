using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeIfElse : NodeBase
    {
        public NodeIfElse(Token start, IEnumerable<(NodeBase condition, NodeBlock block)> ifBlocks, NodeBlock? elseBlock, Token end) : base(start)
        {
            IfElifBlocks = ifBlocks.ToArray();
            End = end;
        }

        public IReadOnlyList<(NodeBase condition, NodeBlock block)> IfElifBlocks { get; }
        public NodeBlock? ElseBlock { get; }

        public Token Start => Token;
        public Token End { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            foreach ((NodeBase condition, NodeBlock block) in IfElifBlocks)
            {
                if (condition.Evaluate(stack).AsBoolean())
                {
                    block.Evaluate(stack);
                    return TuValue.Nothing;
                }
            }

            if (ElseBlock is not null)
            {
                ElseBlock.Evaluate(stack);
            }
            return TuValue.Nothing;
        }
    }
}
