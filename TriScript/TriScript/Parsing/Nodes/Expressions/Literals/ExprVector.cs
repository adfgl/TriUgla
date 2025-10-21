using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprVector : ExprLiteral
    {
        public ExprVector(Token token, Expr size, List<Expr> elements) : base(token)
        {
            Size = size;
            Elements = elements;
        }

        public Expr Size { get; }
        public IReadOnlyList<Expr> Elements { get; }

        public override Value Evaluate(Executor ex)
        {
            int size = Size.Evaluate(ex).integer;
            Value[] values = new Value[size];
            for (int i = 0; i < size; i++)
            {
                values[i] = Elements[i].Evaluate(ex);
            }

            ObjVector vec = new ObjVector();

            Pointer ptr = ex.Heap.Allocate(vec);
            return new Value(ptr);
        }
    }
}
