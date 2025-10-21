using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprCharacter : ExprLiteral
    {
        public ExprCharacter(Token token) 
            : base(token, EDataType.Character)
        {
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string lexeme = source.GetString(Token.span);
            return new Value(lexeme[0]);
        }
    }
}
