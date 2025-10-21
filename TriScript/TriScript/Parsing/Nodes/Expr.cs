using TriScript.Data;
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
    }
}
