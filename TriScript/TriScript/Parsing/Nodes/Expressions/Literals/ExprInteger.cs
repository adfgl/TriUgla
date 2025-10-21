using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprInteger : Expr
    {
        public ExprInteger(Token token) : base(token)
        {

        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return new Value(int.Parse(source.GetString(Token.span)));
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return EDataType.Integer;
        }
    }
}
