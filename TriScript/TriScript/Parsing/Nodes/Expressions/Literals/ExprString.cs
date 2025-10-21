using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprString : Expr
    {
        public ExprString(Token value) : base(value)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string content = source.GetString(Token.span);
            ObjString str = new ObjString(content);
            Pointer ptr = heap.Allocate(str);
            return new Value(ptr);
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return EDataType.String;
        }
    }
}
