using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class Expr 
    {
        protected Expr(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public abstract Value Evaluate(Source source, ScopeStack stack, ObjHeap heap);
        public abstract EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics);

        public abstract bool EvaluateToSI(Source src, ScopeStack stack, ObjHeap heap, DiagnosticBag diagnostics, out double si, out Dimension dim);
    }
}
