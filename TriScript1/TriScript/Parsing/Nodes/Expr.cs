using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class Expr : INode
    {
        protected Expr(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public abstract bool Accept<T>(INodeVisitor<T> visitor, out T? result);
    }
}
