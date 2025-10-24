using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class StmtBlock : Stmt
    {
        public StmtBlock(Token token, List<Stmt> statements) : base(token)
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
