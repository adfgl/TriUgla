using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeExprGroup : NodeBase
    {
        public NodeExprGroup(Token open, NodeBase exp, Token close) : base(open)
        {
            Expression = exp;
            Close = close;
        }

        public Token Open => Token;
        public NodeBase Expression { get; }
        public Token Close { get; }

        public override TuValue Evaluate(TuStack stack) => Expression.Evaluate(stack);

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}
