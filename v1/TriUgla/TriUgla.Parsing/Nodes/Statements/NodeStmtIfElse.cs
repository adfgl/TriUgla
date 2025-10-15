using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtIfElse : NodeStmtBase, IParsableNode<NodeStmtIfElse>
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

        public static NodeStmtIfElse Parse(Parser p)
        {
            HashSet<ETokenType> stop = [ETokenType.ElseIf, ETokenType.Else, ETokenType.EndIf, ETokenType.EOF];

            List<(NodeBase Cond, NodeStmtBlock Block)> elifs = new List<(NodeBase Cond, NodeStmtBlock Block)>();

            Token tkIf = p.Consume(ETokenType.If);
            NodeBase condition = p.ParseExpression();
            NodeStmtBlock ifBlock = p.ParseBlockUntil(tkIf, stop);

            elifs.Add((condition, ifBlock));

            while (p.TryConsume(ETokenType.ElseIf, out var tkElseIf))
            {
                NodeBase elifCond = p.ParseExpression();

                NodeStmtBlock elifBlock = p.ParseBlockUntil(tkElseIf, stop);
                elifs.Add((elifCond, elifBlock));
            }

            NodeStmtBlock? elseBlock = null;
            if (p.TryConsume(ETokenType.Else, out var tkElse))
            {
                elseBlock = p.ParseBlockUntil(tkElse, [ETokenType.EndIf, ETokenType.EOF]);
            }

            return new NodeStmtIfElse(tkIf, elifs, elseBlock, p.Consume(ETokenType.EndIf));
        }
    }
}
