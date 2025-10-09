using TriUgla.Parsing.Compiling.RuntimeObjects;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Scope
    {
        readonly Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();

        public Scope(Scope? parent = null)
        {
            Parent = parent;
        }

        public Scope? Parent { get; }
        public IReadOnlyDictionary<string, Variable> Variables => _variables;

        public void Clear()
        {
            _variables.Clear();
        }

        public bool Resolve(string name, out Scope scope)
        {
            if (_variables.ContainsKey(name))
            {
                scope = this;
                return true;
            }

            if (Parent != null)
            {
                if (Parent.Resolve(name, out scope))
                {
                    return true;
                }
            }
            scope = null!;
            return false;
        }

        public Variable Declare(Token identifer, TuValue value)
        {
            if (Resolve(identifer.value, out _))
            {
                throw new Exception("Already declared");
            }

            Variable variable = new Variable()
            {
                Identifier = identifer,
                Value = value
            };
            _variables.Add(identifer.value, variable);
            return variable;
        }

        public Variable? Get(Token token)
        {
            string name = token.value;
            if (Resolve(name, out Scope scope))
            {
                return scope._variables[name];
            }
            return null;
        }

        public Variable GetOrDeclare(Token token)
        {
            string name = token.value;
            if (Resolve(name, out Scope scope))
            {
                return scope._variables[name];
            }
            return Declare(token, TuValue.Nothing);
        }
    }
}
