namespace TriScript.Data
{
    public sealed class ScopeStack
    {
        private readonly Stack<Scope> _scopes = new Stack<Scope>();
        private Scope? _root;

        public IReadOnlyCollection<Scope> Scopes => _scopes;

        public int LoopDepth { get; private set; } = 0;

        public Scope Current =>
            _scopes.Count != 0 ? _scopes.Peek() : throw new InvalidOperationException("No scopes exist.");

        public Scope Global =>
            _root ?? throw new InvalidOperationException("No global scope exists.");

        public void Clear()
        {
            while (_scopes.Count != 0)
                _scopes.Pop().Clear();
            _root = null;
            LoopDepth = 0;
        }

        public Scope Open()
        {
            var parent = _scopes.Count == 0 ? null : _scopes.Peek();
            var scope = new Scope(parent);
            _scopes.Push(scope);
            _root ??= scope; 
            return scope;
        }

        public void Close()
        {
            if (_scopes.Count == 0)
                throw new InvalidOperationException("No scopes to close.");

            var popped = _scopes.Pop();
            popped.Clear();

            if (_scopes.Count == 0)
                _root = null; // everything gone
        }

        public void EnterLoop() => LoopDepth++;
        public void ExitLoop()
        {
            if (LoopDepth == 0)
                throw new InvalidOperationException("Loop depth underflow.");
            LoopDepth--;
        }

        public bool TryResolve(string name, out Variable variable)
        {
            foreach (var scope in _scopes) // Stack enumerates top -> bottom
                if (scope.TryGetLocal(name, out variable))
                    return true;

            variable = null!;
            return false;
        }

        public IDisposable Guard() => new ScopeGuard(this);

        private sealed class ScopeGuard : IDisposable
        {
            private readonly ScopeStack _stack;
            private bool _disposed;
            public ScopeGuard(ScopeStack stack)
            {
                _stack = stack;
                _stack.Open();
            }
            public void Dispose()
            {
                if (_disposed) return;
                _stack.Close();
                _disposed = true;
            }
        }
    }
}
