using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public class StmtExpr : Stmt
    {
        public StmtExpr(Token token, Expr inner) : base(token)
        {
            Inner = inner;
        }

        public Expr Inner { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
