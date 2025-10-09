using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Functions
{
    public class NodeFunFloor : NodeFun
    {
        public NodeFunFloor(Token name, IEnumerable<INode> args) : base(name, args)
        {
        }

        public override Value Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
