namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtBlock(List<Stmt> statements) : Stmt
    {
        public IReadOnlyList<Stmt> Statements { get; } = statements;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
