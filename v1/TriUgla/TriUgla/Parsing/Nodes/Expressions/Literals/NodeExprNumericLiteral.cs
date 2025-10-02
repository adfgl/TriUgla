using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprNumericLiteral : NodeExprLiteralBase
    {
        public NodeExprNumericLiteral(Token token) : base(token, ETokenType.NumericLiteral)
        {
        }
    }
}
