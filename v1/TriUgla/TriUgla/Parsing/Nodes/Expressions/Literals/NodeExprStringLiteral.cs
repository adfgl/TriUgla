using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprStringLiteral : NodeExprLiteralBase
    {
        public NodeExprStringLiteral(Token token) : base(token, ETokenType.StringLiteral)
        {
        }
    }
}
