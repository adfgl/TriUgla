using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public abstract class NodeLiteralBase : NodeBase
    {
        protected NodeLiteralBase(Token token) : base(token)
        {
        }
    }
}
