using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprGroup : Expr
    {
        public ExprGroup(Token open, Expr expr, Token close)
        {
            Open = open;
            Expr = expr;
            Close = close;
        }

        public Token Open { get; }
        public Expr Expr { get; }
        public Token Close { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap) 
            => Expr.Evaluate(source, stack, heap);
    }
}
