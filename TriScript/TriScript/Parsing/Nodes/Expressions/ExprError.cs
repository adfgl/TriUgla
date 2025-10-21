using TriScript.Data;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprError : Expr
    {
        public override Value Evaluate(Executor ex)
        {
            return Value.Nothing;
        }
    }
}
