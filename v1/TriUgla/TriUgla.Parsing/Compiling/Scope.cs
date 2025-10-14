using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class Scope
    {
        readonly Dictionary<string, NodeBlock> _macros = new Dictionary<string, NodeBlock>();
        readonly Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();

        public Scope(Scope? parent = null)
        {
            Parent = parent;
        }

        public Scope? Parent { get; }
        public IReadOnlyDictionary<string, Variable> Variables => _variables;
        public Dictionary<string, NodeBlock> Macros => _macros;

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

        public Variable Declare(string name, TuValue value)
        {
            if (Resolve(name, out _))
            {
                throw new Exception("Already declared");
            }

            Variable variable = new Variable(name)
            {
                Value = value
            };
            _variables.Add(name, variable);
            return variable;
        }

        public Variable? Get(string name)
        {
            if (Resolve(name, out Scope scope))
            {
                return scope._variables[name];
            }
            return null;
        }

        public Variable GetOrDeclare(string name)
        {
            Variable? existing = Get(name);
            if (existing is not null)
            {
                return existing;
            }
            return Declare(name, TuValue.Nothing);
        }
    }
}
