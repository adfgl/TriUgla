using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprIdentifier : ExprLiteral
    {
        public ExprIdentifier(Token id, EDataType type) : base(id, type) { }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            TextSpan span = Token.span;
            string id = source.GetString(span);
            return stack.Current.Get(id).Value;
        }
    }
}
