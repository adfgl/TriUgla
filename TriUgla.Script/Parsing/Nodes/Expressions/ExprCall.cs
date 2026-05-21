using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprCall(
        Token name,
        Token open,
        List<Expr> args,
        Token close) : Expr
    {
        public Token Name { get; } = name;
        public Token Open { get; } = open;
        public IReadOnlyList<Expr> Args { get; } = args;
        public Token Close { get; } = close;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }
}
