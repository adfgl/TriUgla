using TriScript.Data;

namespace TriScript.Parsing.Nodes.Statements
{
    public class StmtExpr : Stmt
    {
        public StmtExpr(Expr expr)
        {
            Expr = expr;
        }

        public Expr Expr { get; }

        public override void Evaluate(Executor ex)
        {
            Expr.Evaluate(ex);
        }
    }
}
