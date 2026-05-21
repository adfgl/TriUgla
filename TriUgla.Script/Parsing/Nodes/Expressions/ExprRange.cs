namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprRange(Expr start, Expr end, Expr? step) : Expr
    {
        public Expr Start { get; } = start;
        public Expr End { get; } = end;
        public Expr? Step { get; } = step;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
