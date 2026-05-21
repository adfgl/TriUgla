using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprList(Token open, List<Expr> items, Token close) : Expr
    {
        public Token Open { get; } = open;
        public IReadOnlyList<Expr> Items { get; } = items;
        public Token Close { get; } = close;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
