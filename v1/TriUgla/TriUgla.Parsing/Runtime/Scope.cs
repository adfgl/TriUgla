using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Nodes.Statements;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Runtime
{
    public class Scope
    {
        readonly Dictionary<string, NodeStmtBlock> _macros = new Dictionary<string, NodeStmtBlock>();
        readonly Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();

        public Scope(Scope? parent = null)
        {
            Parent = parent;
        }

        public Scope? Parent { get; }
        public IReadOnlyDictionary<string, Variable> Variables => _variables;
        public Dictionary<string, NodeStmtBlock> Macros => _macros;

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

        public Variable Declare(Token token, TuValue value)
        {
            string name = token.value;
            if (Resolve(name, out _))
            {
                throw new Exception("Already declared");
            }

            Variable variable = new Variable(token);
            variable.Unprotect();
            variable.Assign(value);
          
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
    }
}
