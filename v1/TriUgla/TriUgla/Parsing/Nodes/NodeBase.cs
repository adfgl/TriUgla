using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeBase
    {
        public Token Token { get; }

        public NodeBase(Token token)
        {
            Token = token;
        }

        public abstract Value Evaluate(Scope scope);
    }
}
