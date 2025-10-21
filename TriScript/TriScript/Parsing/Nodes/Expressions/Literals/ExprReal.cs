using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprReal : ExprLiteral
    {
        public ExprReal(Token token) 
            : base(token, EDataType.Real)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return new Value(double.Parse(source.GetString(Token.span)));
        }
    }
}
