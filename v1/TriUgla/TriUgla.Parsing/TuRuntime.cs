using System.Collections.ObjectModel;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
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

    public enum EFlowControl { None, Continue, Break }

    public sealed class RuntimeFlow
    {
        private readonly Stack<EFlowControl> _loops = new();

        public bool HasReturn { get; private set; }
        public TuValue ReturnValue { get; private set; } = TuValue.Nothing;

        public void EnterLoop() => _loops.Push(EFlowControl.None);
        public void LeaveLoop() { if (_loops.Count > 0) _loops.Pop(); }

        public void SignalBreak()
        {
            if (_loops.Count == 0) return;                 
            _loops.Pop(); _loops.Push(EFlowControl.Break);
        }
        public void SignalContinue()
        {
            if (_loops.Count == 0) return;                
            _loops.Pop(); _loops.Push(EFlowControl.Continue);
        }
        public void SignalReturn(TuValue v)
        {
            HasReturn = true;
            ReturnValue = v;
        }

        public bool IsBreak => _loops.Count > 0 && _loops.Peek() == EFlowControl.Break;
        public bool IsContinue => _loops.Count > 0 && _loops.Peek() == EFlowControl.Continue;

        public void ConsumeBreakOrContinue()
        {
            if (_loops.Count == 0) return;
            var top = _loops.Pop();
            if (top == EFlowControl.Break || top == EFlowControl.Continue) top = EFlowControl.None;
            _loops.Push(top);
        }
    }

    public class TuRuntime
    {
        readonly NativeFunctions _functions = new NativeFunctions();
        readonly Stack<Scope> _scopes = new Stack<Scope>();
        readonly List<PrintMsg> _printed = new List<PrintMsg>();
        readonly RuntimeFlow _flow = new RuntimeFlow();

        public IReadOnlyCollection<Scope> Scopes => _scopes;
        public IReadOnlyList<PrintMsg> Printed => _printed;
        public NativeFunctions Functions => _functions;
        public RuntimeFlow Flow => _flow;

        public void Print(Token token, string message)
        {
            _printed.Add(new PrintMsg(token, message));
            Console.WriteLine(message);
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

            Variable variable = new Variable(token)
            {
                Value = value
            };
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
