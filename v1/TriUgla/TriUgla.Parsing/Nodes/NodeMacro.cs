using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeMacro : INode
    {
        public NodeMacro(Token macro, INode name, NodeBlock block, Token endMacro)
        {
            Token = Macro = macro;
            Name = name;
            Body = block;
            EndMacro = endMacro;
        }

        public Token Token { get; }
        public INode Name { get; }
        public NodeBlock Body { get; }

        public Token Macro { get; }
        public Token EndMacro { get; }

        public TuValue Accept(INodeVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
