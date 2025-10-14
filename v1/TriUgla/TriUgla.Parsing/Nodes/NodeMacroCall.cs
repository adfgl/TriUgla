using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeMacroCall : INode
    {
        public NodeMacroCall(Token token, INode name)
        {
            Token = token;
            Name = name;
        }

        public Token Token { get; }
        public INode Name { get; }

        public TuValue Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
