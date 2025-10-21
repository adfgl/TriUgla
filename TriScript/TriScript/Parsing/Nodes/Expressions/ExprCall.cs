using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprCall : Expr
    {
        public ExprCall(Token collee, List<Expr> arguments)
        {
            Collee = collee;
            Arguments = arguments;
        }

        public Token Collee { get; }
        public IReadOnlyList<Expr> Arguments { get; }

        public override Value Evaluate(Executor ex)
        {
            string name = ex.Source.GetString(Collee.span);

            int numArgs = Arguments.Count;
            Value[] args = new Value[numArgs];
            for (int i = 0; i < numArgs; i++)
            {
                args[i] = Arguments[i].Evaluate(ex);
            }

            throw new NotImplementedException();
        }
    }
}
