using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtFor(
        Token variable,
        Expr iterable,
        StmtBlock body) : Stmt
    {
        public Token Variable { get; } = variable;
        public Expr Iterable { get; } = iterable;
        public StmtBlock Body { get; } = body;

        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
