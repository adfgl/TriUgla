using TriUgla.Parsing.Data;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Reflection
{
    public class NodeExprNameOf : NodeExprBase
    {
        public NodeExprNameOf(Token token, NodeExprIdentifier id) : base(token)
        {
            Id = id;
        }

        public NodeExprIdentifier Id { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            Id.Evaluate(rt);
            return new TuValue(Id.Variable!.Name);
        }
    }
}
