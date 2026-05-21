using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Statements
{
    public sealed class StmtError(Token token, string message) : Stmt
    {
        public Token Token { get; } = token;
        public string Message { get; } = message;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }
}
