using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Functions
{
    public abstract class NodeFun : INode
    {
        public NodeFun(Token name, IEnumerable<INode> args)
        {
            Name = name;
            Args = args.ToArray();
        }

        public Token Name { get; }
        public IReadOnlyList<INode> Args { get; }

        public abstract Value Accept(INodeVisitor visitor);
    }
}
