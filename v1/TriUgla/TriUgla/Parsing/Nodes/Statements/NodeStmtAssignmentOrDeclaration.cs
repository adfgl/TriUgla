using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Exceptions;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtAssignmentOrDeclaration : NodeStmtBase
    {
        public NodeExprBase Identifier { get; }
        public NodeExprBase? Expression { get; }

        public NodeStmtAssignmentOrDeclaration(Token token, NodeExprBase identifier, NodeExprBase? expression) : base(token)
        {
            Identifier = identifier;
            Expression = expression;
        }

        public override Value Evaluate(Scope scope)
        {
            EDataType type = EDataType.None;

            Variable variable = scope.Get(Identifier.Token, out bool fetched, true, type);
            if (Expression is null)
            {
                return variable.Value;
            }
            else
            {
                Value value = Expression.Evaluate(scope);
                variable.Assign(value);
            }
            return variable.Value;
        }
    }
}
