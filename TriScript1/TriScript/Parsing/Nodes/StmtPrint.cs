using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class StmtPrint : Stmt
    {
        public StmtPrint(Token token, List<Expr> arguments) : base(token)
        {
            Arguments = arguments;
        }

        public IReadOnlyList<Expr> Arguments { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
