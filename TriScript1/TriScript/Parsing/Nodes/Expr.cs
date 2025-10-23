using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class Expr
    {
        protected Expr(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public abstract T Accept<T>(IExprVisitor<T> visitor);
    }
}
