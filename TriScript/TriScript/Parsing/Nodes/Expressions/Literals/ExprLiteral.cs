using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public abstract class ExprLiteral : Expr
    {
        protected ExprLiteral(Token token)
        {
            Token = token;
        }

        public Token Token {get; }
    }
}
