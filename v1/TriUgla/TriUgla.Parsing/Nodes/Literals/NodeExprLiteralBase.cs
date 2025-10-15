using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public abstract class NodeExprLiteralBase : NodeBase
    {
        protected NodeExprLiteralBase(Token token) : base(token)
        {
        }
    }
}
