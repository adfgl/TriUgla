using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprAssignmentByIndex : Expr
    {
        public ExprAssignmentByIndex(Token target, List<Expr> index, Expr value)
        {
            Target = target;
            Index = index;
            Value = value;
        }

        public Token Target { get; }
        public IReadOnlyList<Expr> Index { get; }
        public Expr Value { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            throw new NotImplementedException();
        }
    }
}
