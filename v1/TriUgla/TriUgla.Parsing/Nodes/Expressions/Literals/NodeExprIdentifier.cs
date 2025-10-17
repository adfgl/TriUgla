using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public class NodeExprIdentifier : NodeExprLiteralBase
    {
        Variable? _variable = null;

        public NodeExprIdentifier(Token token, bool isTuple, NodeExprBase? index) : base(token)
        {
            IsTuple = isTuple;
            Index = index;
        }

        public bool IsTuple { get; }
        public NodeExprBase? Index { get; }
        public Variable? Variable => _variable;
        public bool DeclareIfMissing { get; set; } = false;

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            if (_variable is not null)
            {
                return _variable.Value;
            }

            string name = Token.value;
            if (Index is not null)
            {
                TuValue index = Index.Evaluate(rt);
                if (index.type != EDataType.Integer)
                {
                    throw new CompileTimeException("Expected index.", Index.Token);
                }
                name += $"_{index.AsInteger()}";
            }

            if (!ValidIdentifier(name, out string reason))
            {
                throw new CompileTimeException(
                      $"Invalid identifier name '{name}': {reason}.",
                      Token);
            }

            _variable = rt.Current.Get(name);
            if (DeclareIfMissing && _variable is null)
            {
                _variable = rt.Current.Declare(new Token(Token, name), TuValue.Nothing);
            }

            if (_variable is null)
            {
                throw new CompileTimeException($"Undefined variable '{name}'.", Token);
            }

            TuValue value = _variable.Value;
            if (IsTuple && value.type != EDataType.Nothing && value.type != EDataType.List)
            {
                throw new CompileTimeException("Expected tuple", Token);
            }
            return value;
        }
    }
}
