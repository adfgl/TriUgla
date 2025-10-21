using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprGroup : Expr
    {
        public ExprGroup(Token open, Expr expr, Token close) : base(open)
        {
            Expr = expr;
            Close = close;
        }

        public Token Open => Token;
        public Expr Expr { get; }
        public Token Close { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap) 
            => Expr.Evaluate(source, stack, heap);

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return Expr.PreviewType(source, stack, diagnostics);
        }
    }
}
