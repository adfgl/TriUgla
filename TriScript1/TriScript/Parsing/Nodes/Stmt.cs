using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class Stmt : INode
    {
        protected Stmt(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public abstract bool Accept<T>(INodeVisitor<T> visitor, out T? result);
    }
}
