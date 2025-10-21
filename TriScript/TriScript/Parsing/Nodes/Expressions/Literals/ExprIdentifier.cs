using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprIdentifier : Expr
    {
        public ExprIdentifier(Token id) : base(id) { }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string id = source.GetString(Token.span);
            return stack.Current.Get(id).Value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            string id = source.GetString(Token.span);
            if (stack.Current.TryGet(id, out Variable var))
            {
                return var.Value.type;
            }
            return EDataType.None;
        }
    }
}
