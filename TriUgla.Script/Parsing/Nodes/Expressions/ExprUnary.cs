using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprUnary(Token op, Expr right) : Expr
    {
        public Token Op { get; } = op;
        public Expr Right { get; } = right;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
