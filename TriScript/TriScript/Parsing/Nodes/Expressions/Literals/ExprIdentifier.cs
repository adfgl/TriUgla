using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprIdentifier : ExprLiteral
    {
        public ExprIdentifier(Token id) : base(id) { }

        public override Value Evaluate(Executor ex)
        {
            TextSpan span = Token.span;
            string id = ex.Source.GetString(span);
            return ex.CurrentScope.Get(id).Value;
        }
    }
}
