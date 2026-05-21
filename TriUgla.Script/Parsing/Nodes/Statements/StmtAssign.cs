using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtAssign(Expr target, Token op, Expr value) : Stmt
    {
        public Expr Target { get; } = target;
        public Token Op { get; } = op;
        public Expr Value { get; } = value;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
