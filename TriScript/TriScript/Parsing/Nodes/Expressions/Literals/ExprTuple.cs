using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprTuple : ExprLiteral
    {
        public ExprTuple(Token token, List<Expr> elements) : base(token)
        {
            Elements = elements;
        }

        public IReadOnlyList<Expr> Elements { get; }

        public override Value Evaluate(Executor ex)
        {
            int size = Elements.Count;
            Value[] values = new Value[size];
            for (int i = 0; i < size; i++)
            {
                values[i] = Elements[i].Evaluate(ex);
            }
            return new Value();
        }
    }
}
