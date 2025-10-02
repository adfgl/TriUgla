using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Exceptions;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprUnary : NodeExprBase
    {
        public NodeExprBase Expression { get; }

        public NodeExprUnary(Token token, NodeExprBase expression) : base(token)
        {
            Expression = expression;
        }

        public override Value Evaluate(Scope scope)
        {
            Value exp = Expression.Evaluate(scope);
            switch (Token.type)
            {
                case ETokenType.Minus:
                    if (exp.type == EDataType.Numeric)
                    {
                        return new Value(-exp.numeric);
                    }
                    break;

                case ETokenType.Not:
                    if (exp.type == EDataType.Numeric)
                    {
                        return new Value((exp.numeric == 0.0) ? 1.0 : 0.0);
                    }
                    break;
            }
            throw new InvalidOperatorException(Token, exp);
        }

        public override string ToString()
        {
            return $"{Token.type} {Expression}";
        }
    }
}
