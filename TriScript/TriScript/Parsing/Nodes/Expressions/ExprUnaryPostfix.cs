using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprUnaryPostfix : Expr
    {
        public ExprUnaryPostfix(Expr expr, Token op)
        {
            Expr = expr;
            Operator = op;
        }

        public Expr Expr { get; }
        public Token Operator { get; }

        public override Value Evaluate(Executor ex)
        {
            throw new NotImplementedException();
        }
    }
}
