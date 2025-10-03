namespace TriUgla.Parsing.Compiling
{
    public class Stack
    {
        readonly Stack<Scope> _scopes = new Stack<Scope>();

        public Stack()
        {
            OpenScope();
        }

        public Scope Current => _scopes.Count == 0 ? null! : _scopes.Peek();
        public IReadOnlyCollection<Scope> Scopes => _scopes;

        public Scope OpenScope()
        {
            Scope scope = new Scope(Current);
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
