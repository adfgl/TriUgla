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
            throw new NotImplementedException();
        }
    }
}
