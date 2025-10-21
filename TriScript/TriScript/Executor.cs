using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Parsing;
using TriScript.Parsing.Nodes;

namespace TriScript
{
    public class Executor
    {
        readonly Stack<Scope> _scopes = new Stack<Scope>();
        readonly ObjHeap _heap = new ObjHeap();
        readonly Source _source;

        public Executor(Source source)
        {
            _source = source;
        }

        public IReadOnlyCollection<Scope> Scopes => _scopes;
        public ObjHeap Heap => _heap;
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
            DiagnosticBag diagnos = new DiagnosticBag();
            Parser parser = new Parser(_source, diagnos);

            TriProgram program = parser.Parse();

            foreach (Diagnostic item in diagnos.Items)
            {
                Console.WriteLine(item);
            }

            if (!diagnos.HasErrors)
            {
                program.Evaluate(this);
            }
        }
    }
}
