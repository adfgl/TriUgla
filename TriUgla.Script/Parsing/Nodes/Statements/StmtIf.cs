namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtIf(
        Expr condition,
        StmtBlock thenBlock,
        List<StmtElseIf> elseIfs,
        StmtBlock? elseBlock) : Stmt
    {
        public Expr Condition { get; } = condition;
        public StmtBlock ThenBlock { get; } = thenBlock;
        public IReadOnlyList<StmtElseIf> ElseIfs { get; } = elseIfs;
        public StmtBlock? ElseBlock { get; } = elseBlock;

        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
