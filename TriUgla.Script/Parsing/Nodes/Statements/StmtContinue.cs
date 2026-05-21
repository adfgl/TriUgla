using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtContinue(Token token) : Stmt
    {
        public Token Token { get; } = token;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
