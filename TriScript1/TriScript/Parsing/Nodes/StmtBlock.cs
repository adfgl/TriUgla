namespace TriScript.Parsing.Nodes
{
    public sealed class StmtBlock : Stmt
    {
        public StmtBlock(List<Stmt> statements)
        {
            Statements = statements;
        }

        public IReadOnlyList<Stmt> Statements { get; }
    }
}
