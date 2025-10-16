using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public abstract class NodeExprLiteralBase : NodeExprBase
    {
        protected NodeExprLiteralBase(Token token) : base(token)
        {
        }
    }
}
