using TriScript.Data;
using TriScript.Data.Units;
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

        public override bool EvaluateToSI(Source src, ScopeStack stack, ObjHeap heap, DiagnosticBag diagnostics, out double si, out Dimension dim)
        {
            throw new NotImplementedException();
        }
    }
}
