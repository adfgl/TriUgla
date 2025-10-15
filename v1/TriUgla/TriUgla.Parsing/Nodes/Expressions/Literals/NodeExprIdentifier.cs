using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public class NodeExprIdentifier : NodeExprLiteralBase
    {
        public NodeExprIdentifier(Token token) : base(token)
        {
            Name = token.value;
        }

        public string Name { get; private set; }
        public bool DeclareIfMissing { get; set; } = false;

        protected override TuValue Evaluate(TuRuntime stack)
        {
            if (!ValidIdentifier(Name, out string reason))
            {
                throw new CompileTimeException(
                      $"Invalid identifier name '{Name}': {reason}.",
                      Token);
            }

            Variable? v = stack.Current.Get(Name);
            if (DeclareIfMissing && v is null)
            {
                v = stack.Current.Declare(Token, TuValue.Nothing);
            }

            if (v is null)
            {
                throw new CompileTimeException($"Undefined variable '{Name}'.", Token);
            }
            return v.Value;
        }
    }
}
