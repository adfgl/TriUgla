using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprIdentifierLiteral : NodeExprLiteralBase
    {
        public string Name { get; }

        public NodeExprIdentifierLiteral(Token token) : base(token, ETokenType.IdentifierLiteral)
        {
            Name = token.value.ToString();
        }

        public override Value Evaluate(Scope scope)
        {
            Value value = scope.Get(Token, out bool fetched, false).Value;
            return value;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
