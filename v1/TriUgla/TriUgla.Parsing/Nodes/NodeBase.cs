using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeBase
    {
        public NodeBase(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public abstract TuValue Evaluate(TuStack stack);
    }
}
