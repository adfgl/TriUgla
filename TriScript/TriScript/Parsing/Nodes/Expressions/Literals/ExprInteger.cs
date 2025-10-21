using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprInteger : ExprLiteral
    {
        public ExprInteger(Token token)
            : base(token, EDataType.Integer)
        {

        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return new Value(int.Parse(source.GetString(Token.span)));
        }
    }
}
