using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprList : ExprLiteral
    {
        public ExprList(Token token, List<Expr> expressions) : base(token)
        {
        }

        public override Value Evaluate(Executor ex)
        {
            throw new NotImplementedException();
        }
    }
}
