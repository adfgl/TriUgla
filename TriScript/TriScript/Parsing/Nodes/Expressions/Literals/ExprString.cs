using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprString : Expr
    {
        public ExprString(Token value)
        {
            Token = value;
        }

        public Token Token { get; }

        public override Value Evaluate(Executor ex)
        {
            string content = ex.Source.GetString(Token.span);
            ObjString str = new ObjString(content);
            Pointer ptr = ex.Heap.Allocate(str);
            return new Value(ptr);
        }
    }
}
