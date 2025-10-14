using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeString : INode
    {
        public NodeString(Token token)
        {
            Token = token;
        }

        public Token Token { get; }

        public TuValue Accept(INodeEvaluationVisitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return Token.value;
        }
    }
}
