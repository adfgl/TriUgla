namespace TriScript.Data
{
    public sealed class Scope
    {
        private readonly Dictionary<string, Variable> _variables =
            new Dictionary<string, Variable>(StringComparer.Ordinal);

        public Scope(Scope? parent = null) => Parent = parent;

        public Scope? Parent { get; }
        public IReadOnlyDictionary<string, Variable> Variables => _variables;

        public void Clear() => _variables.Clear();

        public bool Shadows(string name) =>
            Parent != null && (Parent.Contains(name) || Parent.Shadows(name));

        public bool Contains(string name) => _variables.ContainsKey(name);

        // Resolve within this chain (iterative).
        public bool TryResolve(string name, out Variable variable)
        {
            for (Scope? s = this; s != null; s = s.Parent)
            {
                if (s._variables.TryGetValue(name, out variable))
                    return true;
            }
            variable = null!;
            return false;
        }

        public bool TryGetLocal(string name, out Variable variable) =>
            _variables.TryGetValue(name, out variable);

        public bool TryGet(string name, out Variable variable) =>
            TryResolve(name, out variable);

        public Variable Get(string name) =>
            TryResolve(name, out var v)
                ? v
                : throw new KeyNotFoundException($"Variable '{name}' is not declared in any accessible scope.");

        public Variable Declare(Variable variable)
        {
            if (_variables.ContainsKey(variable.Name))
                throw new InvalidOperationException($"Variable '{variable.Name}' is already declared in this scope.");
            _variables[variable.Name] = variable;
            return variable;
        }
    }
}
