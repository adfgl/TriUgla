using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Assignment
{
    public sealed class NodeExprAssignment : NodeExprBase
    {
        public NodeExprAssignment(NodeExprIdentifier id, Token op, NodeExprBase expression) : base(op)
        {
            Assignee = id;
            Expression = expression;
        }

        public NodeExprIdentifier Assignee { get; }
        public NodeExprBase Expression { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            Assignee.DeclareIfMissing = true;
            TuValue current = Assignee.Evaluate(rt);
            TuValue toSet = Expression.Evaluate(rt);
            Assignee.Variable!.Assign(toSet);
            return toSet;
        }

    }
}
