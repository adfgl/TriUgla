namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtWhile(
        Expr condition,
        StmtBlock body) : Stmt
    {
        public Expr Condition { get; } = condition;
        public StmtBlock Body { get; } = body;

        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
