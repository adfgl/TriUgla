using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprGroup : NodeExprBase
    {
        public NodeExprBase Expression { get; }

        public NodeExprGroup(Token token, NodeExprBase expression) : base(token)
        {
            Expression = expression;
        }

        public override Value Evaluate(Scope scope)
        {
            return Expression.Evaluate(scope);
        }

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}
