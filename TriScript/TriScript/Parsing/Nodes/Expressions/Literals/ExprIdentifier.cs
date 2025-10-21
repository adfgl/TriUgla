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
            string content = ex.Source.GetString(span);

            if (ex.CurrentScope.TryGet(content, out Variable value))
            {
                return value.Value;
            }
            throw new Exception();
        }
    }
}
