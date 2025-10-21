using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprError : Expr
    {
        public ExprError(Token token) : base(token)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return Value.Nothing;
        }

        public override EDataType PreviewType(Source source, ScopeStack stackdiagnostics, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }
    }
}
