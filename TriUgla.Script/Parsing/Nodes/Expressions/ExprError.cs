using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Nodes.Expressions
{
    public sealed class ExprError(Token token, string message) : Expr
    {
        public Token Token { get; } = token;
        public string Message { get; } = message;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }
}
