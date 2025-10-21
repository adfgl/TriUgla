using TriScript.Data;
using TriScript.Diagnostics;

namespace TriScript
{
    public class Executor
    {
        readonly DiagnosticBag _diagnostic = new DiagnosticBag();
        readonly Stack<Scope> _scopes = new Stack<Scope>();
        readonly ObjHeap _heap = new ObjHeap();
        readonly Source _source;

        public Executor(Source source)
        {
            _source = source;
        }

        public IReadOnlyCollection<Scope> Scopes => _scopes;
        public ObjHeap Heap => _heap;
        public DiagnosticBag Diagnostic => _diagnostic;
        public Source Source => _source;

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

        public void Run()
        {

        }
    }
}
