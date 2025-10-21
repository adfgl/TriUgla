using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprMatrix : ExprLiteral
    {
        public ExprMatrix(Token token, Expr rows, Expr cols, List<List<Expr>> rowRecors) : base(token)
        {
            Rows = rows;
            Cols = cols;
            RowRecors = rowRecors;
        }
        
        public Expr Rows { get; }
        public Expr Cols { get; }
        public IReadOnlyList<IReadOnlyList<Expr>> RowRecors { get; }

        public override Value Evaluate(Executor ex)
        {
            int rows = Rows.Evaluate(ex).integer;
            int cols = Cols.Evaluate(ex).integer;

            throw new Exception();
        }
    }
}
