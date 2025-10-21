using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public abstract class ExprLiteral : Expr
    {
        protected ExprLiteral(Token token, EDataType type)
        {
            Token = token;
            Type = type;
        }

        public Token Token {get; }
        public EDataType Type { get; }
    }
}
