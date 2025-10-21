using TriScript.Data;

namespace TriScript.Parsing.Nodes.Statements
{
    public class StmtIfElse : Stmt
    {
        public StmtIfElse(List<(Expr, StmtBlock)> ifBlocks, StmtBlock? elseBlock)
        {
            this.IfBlocks = ifBlocks;
            ElseBlock = elseBlock;
        }

        public IReadOnlyCollection<(Expr, StmtBlock)> IfBlocks { get; }
        public StmtBlock? ElseBlock { get; }

        public override void Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            foreach ((Expr condtion, StmtBlock block) in IfBlocks)
            {
                Value value = condtion.Evaluate(source, stack, heap);
                if (value.boolean)
                {
                    block.Evaluate(source, stack, heap);
                    return;
                }
            }
            ElseBlock?.Evaluate(source, stack, heap);
        }
    }
}
