using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprGroup(Token open, Expr inner, Token close) : Expr
    {
        public Token Open { get; } = open;
        public Expr Inner { get; } = inner;
        public Token Close { get; } = close;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
