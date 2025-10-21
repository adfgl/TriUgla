using TriScript.Data;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprError : Expr
    {
        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return Value.Nothing;
        }
    }
}
