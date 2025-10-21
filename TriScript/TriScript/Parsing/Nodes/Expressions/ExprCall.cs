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

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string name = source.GetString(Collee.span);

            int numArgs = Arguments.Count;
            Value[] args = new Value[numArgs];
            for (int i = 0; i < numArgs; i++)
            {
                args[i] = Arguments[i].Evaluate(source, stack, heap);
            }

            throw new NotImplementedException();
        }
    }
}
