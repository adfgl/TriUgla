namespace TriScript.Parsing.Nodes
{
    public sealed class StmtBlock : Stmt
    {
        public StmtBlock(List<Stmt> statements)
        {
            Statements = statements;
        }

        public IReadOnlyList<Stmt> Statements { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
