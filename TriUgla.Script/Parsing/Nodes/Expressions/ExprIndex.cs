using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprIndex(
        Expr target,
        Token open,
        Expr? index,
        Token close) : Expr
    {
        public Expr Target { get; } = target;
        public Token Open { get; } = open;
        public Expr? Index { get; } = index;
        public Token Close { get; } = close;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }
}
