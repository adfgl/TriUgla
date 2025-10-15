using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtIfElse : NodeBase
    {
        public NodeStmtIfElse(Token start, IEnumerable<(NodeBase condition, NodeStmtBlock block)> ifBlocks, NodeStmtBlock? elseBlock, Token end) : base(start)
        {
            IfElifBlocks = ifBlocks.ToArray();
            End = end;
        }

        public IReadOnlyList<(NodeBase condition, NodeStmtBlock block)> IfElifBlocks { get; }
        public NodeStmtBlock? ElseBlock { get; }

        public Token Start => Token;
        public Token End { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue result;
            bool checkElse = true;
            foreach ((NodeBase condition, NodeStmtBlock block) in IfElifBlocks)
            {
                if (condition.Evaluate(stack).AsBoolean())
                {
                    result = block.Evaluate(stack);
                    checkElse = false;
                    break;
                }
            }

            if (checkElse && ElseBlock is not null)
            {
                result = ElseBlock.Evaluate(stack);
            }
            return TuValue.Nothing;
        }
    }
}
