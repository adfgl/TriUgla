using System.Collections.ObjectModel;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Compiling
{
    public class PrintMsg
    {
        public PrintMsg(Token token, string message)
        {
            Token = token;
            Message = message;
        }

        public Token Token { get; set; }
        public string Message { get; set; }
    }

    public class TuStack
    {
        readonly NativeFunctions _functions;
        readonly Stack<Scope> _scopes;
        readonly List<PrintMsg> _printed;

        public TuStack()
        {
            _functions = new NativeFunctions();
            _scopes = new Stack<Scope>();
            _printed = new List<PrintMsg>();
        }

        public IReadOnlyCollection<Scope> Scopes => _scopes;
        public IReadOnlyList<PrintMsg> Printed => _printed;
        public NativeFunctions Functions => _functions;

        public void Print(Token token, string message)
        {
            _printed.Add(new PrintMsg(token, message));
        }

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
