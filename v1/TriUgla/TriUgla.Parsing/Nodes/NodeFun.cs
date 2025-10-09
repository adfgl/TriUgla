using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeFun : INode
    {
        public NodeFun(Token name, IEnumerable<INode> args)
        {
            Token = name;
            Args = args.ToArray();
        }

        public Token Token { get; }
        public IReadOnlyList<INode> Args { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Token.value}({Args.Count})";
        }
    }
}
