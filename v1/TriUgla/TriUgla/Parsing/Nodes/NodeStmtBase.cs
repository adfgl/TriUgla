using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeStmtBase : NodeBase
    {
        public NodeStmtBase(Token token) : base(token)
        {
        }
    }
}
