using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprCharacter : Expr
    {
        public ExprCharacter(Token token) : base(token)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string lexeme = source.GetString(Token.span);
            return new Value(lexeme[0]);
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return EDataType.Character;
        }
    }
}
