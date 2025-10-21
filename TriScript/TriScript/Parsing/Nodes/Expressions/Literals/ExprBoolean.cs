using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprBoolean : ExprLiteral
    {
        public ExprBoolean(Token token) 
            : base(token, EDataType.Boolean)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return new Value(Token.type == ETokenType.True);
        }
    }
}
