namespace TriScript.Data
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

        public bool Shadows(string name)
        {
            if (Parent is null || !_variables.ContainsKey(name))
            {
                return false;
            }
            return Parent.Shadows(name);
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

        public Variable Declare(Variable variable)
        {
            _variables.Add(variable.Name, variable);
            return variable;
        }

        public bool TryGet(string name, out Variable variable)
        {
            if (Resolve(name, out Scope scope))
            {
                variable = scope._variables[name];
                return true;
            }
            variable = null!; 
            return false;
        }
    }
}
