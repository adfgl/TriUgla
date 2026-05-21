using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprString(Token token, string value) : Expr
    {
        public Token Token { get; } = token;
        public string Value { get; } = value;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
