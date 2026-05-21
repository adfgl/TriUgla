using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprBoolean(Token token, bool value) : Expr
    {
        public Token Token { get; } = token;
        public bool Value { get; } = value;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
