using TriScript.Data;
using TriScript.Data.Objects;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprMatrix : ExprLiteral
    {
        public ExprMatrix(Token token, Expr rows, Expr cols, List<Expr> rowRecors) : base(token)
        {
            Rows = rows;
            Cols = cols;
            RowRecors = rowRecors;
        }

        public Expr Rows { get; }
        public Expr Cols { get; }
        public IReadOnlyList<Expr> RowRecors { get; }

        public override Value Evaluate(Executor ex)
        {
            int rows = Rows.Evaluate(ex).integer;
            int cols = Cols.Evaluate(ex).integer;

            int size = rows * cols;
            Value[] values = new Value[size];
            for (int i = 0; i < size; i++)
            {
                values[i] = RowRecors[i].Evaluate(ex);
            }

            ObjMatrix mat = new ObjMatrix(rows, cols);

            Pointer ptr = ex.Heap.Allocate(mat);
            return new Value(ptr);
        }
    }
}
