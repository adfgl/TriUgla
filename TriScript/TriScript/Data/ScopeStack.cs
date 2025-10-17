namespace TriScript.Data
{
    public class ScopeStack
    {
        readonly Stack<Scope> _scopes = new Stack<Scope>();
        int _loopDepth = 0;

        public IReadOnlyCollection<Scope> Scopes => _scopes;
        public int LoopDepth => _loopDepth;

        public Scope CurrentScope
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

        public Scope GlobalScope
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
            _loopDepth++;
            return scope;
        }

        public void CloseScope()
        {
            if (_scopes.Count == 1)
            {
                return;
            }
            _scopes.Pop();
            _loopDepth--;
        }
    }
}
