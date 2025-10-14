using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeMacro : INode
    {
        public NodeMacro(Token token, INode name, NodeBlock block)
        {
            Token = token;
            Name = name;
            Body = block;
        }

        public Token Token { get; }
        public INode Name { get; }
        public NodeBlock Body { get; }

        public TuValue Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
