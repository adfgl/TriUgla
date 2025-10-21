using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprUnaryPostfix : Expr
    {
        public ExprUnaryPostfix(Expr expr, Token op) : base(op)
        {
            Expr = expr;
        }

        public Expr Expr { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            throw new NotImplementedException();
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }
    }
}
