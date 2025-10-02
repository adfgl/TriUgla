using TriUgla.Parsing.Exceptions;

namespace TriUgla.Parsing
{
    public class Scope
    {
        readonly Dictionary<string, Variable> m_variables = new Dictionary<string, Variable>();

        public IReadOnlyDictionary<string, Variable> Variables => m_variables;

        public void Clear()
        {
            m_variables.Clear();
        }

        public bool Resolve(string name, out Scope scope)
        {
            if (m_variables.ContainsKey(name))
            {
                scope = this;
                return true;
            }
            scope = null!;
            return false;
        }

        public bool TryGet(string name, out Variable? variable)
        {
            if (Resolve(name, out Scope scope))
            {
                variable = scope.m_variables[name];
                return true;
            }
            variable = null;
            return false;
        }

        public Variable Get(Token name, out bool fetched, bool declareIfMissing = false, EDataType dataType = EDataType.None)
        {
            string id = name.value.ToString();
            if (Resolve(id, out Scope scope))
            {
                fetched = true;
                return scope.m_variables[id];
            }

            if (declareIfMissing)
            {
                Variable variable = new Variable(name, dataType);
                Declare(variable);
                fetched = false;
                return variable;
            }
            else
            {
                throw new UseOfUndeclaredVariableException(name);
            }
        }

        public void Declare(Variable variable)
        {
            if (Resolve(variable.Name, out Scope scope))
            {
                throw new VariableAlreadyDeclaredException(variable.Token, scope == this);
            }
            m_variables.Add(variable.Name, variable);
        }
    }
}
