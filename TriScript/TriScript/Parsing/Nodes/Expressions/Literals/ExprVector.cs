using TriScript.Data;
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
            throw new NotImplementedException();
        }
    }
}
