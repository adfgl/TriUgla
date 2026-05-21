namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtExpr(Expr expr) : Stmt
    {
        public Expr Expr { get; } = expr;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
