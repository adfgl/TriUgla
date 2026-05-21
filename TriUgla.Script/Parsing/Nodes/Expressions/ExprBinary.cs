using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprBinary(Expr left, Token op, Expr right) : Expr
    {
        public Expr Left { get; } = left;
        public Token Op { get; } = op;
        public Expr Right { get; } = right;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
