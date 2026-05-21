namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtElseIf(
        Expr condition,
        StmtBlock block) : Stmt
    {
        public Expr Condition { get; } = condition;
        public StmtBlock Block { get; } = block;

        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
