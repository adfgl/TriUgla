using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprIdentifier(Token token) : Expr
    {
        public Token Token { get; } = token;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
