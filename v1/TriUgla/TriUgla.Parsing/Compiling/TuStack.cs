using System.Collections.ObjectModel;
using TriUgla.Parsing.Nodes.FlowControl;

namespace TriUgla.Parsing.Compiling
{
    public class TuStack
    {

        readonly Dictionary<string, NodeBlock> _macros = new();
        readonly Stack<Scope> _scopes = new Stack<Scope>();

        public IReadOnlyCollection<Scope> Scopes => _scopes;
        public Dictionary<string, NodeBlock> Macros => _macros;

        public Scope Current
        {
            get
            {
                if (_scopes.Count == 0)
                {
                    throw new InvalidOperationException("No scopes exist.");
                }
                return _scopes.Peek();
            }
        }

        public Scope Global
        {
            get
            {
                if (_scopes.Count == 0)
                {
                    throw new InvalidOperationException("No scopes exist.");
                }
                return _scopes.Last();
            }
        }

        public Scope OpenScope()
        {
            Scope scope = new Scope(_scopes.Count == 0 ? null : _scopes.Peek());
            _scopes.Push(scope);
            return scope;
        }

        public void CloseScope()
        {
            if (_scopes.Count == 1)
            {
                return;
            }
            _scopes.Pop();
        }
    }
}
