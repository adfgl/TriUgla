using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
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

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue value = Expression.Evaluate(stack);
            if (value.type == EDataType.Nothing)
            {
                if (Expression is NodeExprIdentifier id)
                {
                    throw new CompileTimeException(
                        $"Grouped expression cannot be 'Nothing': variable '{id.Name}' is undefined or uninitialized.",
                        id.Token);
                }

                throw new RunTimeException(
                    "Grouped expression evaluated to 'Nothing'.",
                    Expression.Token);
            }
            return value;
        }

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}
