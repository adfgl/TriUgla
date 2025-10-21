using TriScript.Data;
using TriScript.Parsing.Nodes.Expressions.Literals;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprIndex : Expr
    {
        public ExprIndex(Expr callee, Expr row, Expr? col)
        {
            Callee = callee;
            Row = row;
            Col = col;
        }

        public Expr Callee { get; }
        public Expr Row { get; }
        public Expr? Col { get; }

        public override Value Evaluate(Executor ex)
        {
            Value list = Callee.Evaluate(ex);

            int row = Row.Evaluate(ex).integer;
            int col = Col is null ? -1 : Col.Evaluate(ex).integer;

            if (list.type == EDataType.Matrix)
            {

            }
            return new Value();
        }
    }
}
