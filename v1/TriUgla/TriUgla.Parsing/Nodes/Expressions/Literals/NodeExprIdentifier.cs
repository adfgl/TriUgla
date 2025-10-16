using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public class NodeExprIdentifier : NodeExprLiteralBase
    {
        Variable? _variable = null;

        public NodeExprIdentifier(Token token) : base(token)
        {
        }

        public Variable? Variable => _variable;
        public bool DeclareIfMissing { get; set; } = false;

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            if (_variable is not null)
            {
                return _variable.Value;
            }

            string name = Token.value;
            if (!ValidIdentifier(name, out string reason))
            {
                throw new CompileTimeException(
                      $"Invalid identifier name '{name}': {reason}.",
                      Token);
            }

            _variable = stack.Current.Get(name);
            if (DeclareIfMissing && _variable is null)
            {
                _variable = stack.Current.Declare(Token, TuValue.Nothing);
            }

            if (_variable is null)
            {
                throw new CompileTimeException($"Undefined variable '{name}'.", Token);
            }
            return _variable.Value;
        }
    }
}
