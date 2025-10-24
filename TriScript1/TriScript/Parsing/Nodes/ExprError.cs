using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public class ExprError : Expr
    {
        public ExprError(Token token) : base(token)
        {
        }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            throw new NotImplementedException();
        }
    }
}
