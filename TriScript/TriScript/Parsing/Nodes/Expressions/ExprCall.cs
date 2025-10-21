using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprCall : Expr
    {
        public ExprCall(Token collee, List<Expr> arguments) : base(collee)
        {
            Arguments = arguments;
        }

        public IReadOnlyList<Expr> Arguments { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string name = source.GetString(Token.span);

            int numArgs = Arguments.Count;
            Value[] args = new Value[numArgs];
            for (int i = 0; i < numArgs; i++)
            {
                args[i] = Arguments[i].Evaluate(source, stack, heap);
            }

            throw new NotImplementedException();
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }
    }
}
