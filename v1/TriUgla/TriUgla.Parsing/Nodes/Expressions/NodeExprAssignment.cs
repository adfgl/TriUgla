using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprAssignment : NodeExprBase
    {
        public NodeExprAssignment(NodeExprBase id, Token op, NodeExprBase expression) : base(op)
        {
            Assignee = id;
            Expression = expression;
        }

        public NodeExprBase Assignee { get; }
        public NodeExprBase Expression { get; }

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            if (Assignee is NodeExprIdentifier id)
            {
                TuValue value = Expression.Evaluate(stack);
                id.DeclareIfMissing = true;
                TuValue current = id.Evaluate(stack);
                id.Variable!.Assign(value);
                return id.Variable.Value;
            }
            throw new Exception();
        }
    }
}
