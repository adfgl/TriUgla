using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprString : ExprLiteral
    {
        public ExprString(Token value) 
            : base(value, EDataType.String)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string content = source.GetString(Token.span);
            ObjString str = new ObjString(content);
            Pointer ptr = heap.Allocate(str);
            return new Value(ptr);
        }
    }
}
