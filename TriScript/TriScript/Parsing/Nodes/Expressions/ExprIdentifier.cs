using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprIdentifier : Expr
    {
        public ExprIdentifier(Token id)
        {
            Id = id;
        }

        public Token Id { get; }

        public override Value Evaluate(Executor rt)
        {
            TextSpan span = Id.span;
            string content = rt.Source.GetString(span);

            if (rt.CurrentScope.TryGet(content, out var value))
            {
                return value.Value;
            }
            throw new Exception();
        }
    }
}
