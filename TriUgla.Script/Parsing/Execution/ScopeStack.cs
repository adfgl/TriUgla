using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Parsing.Execution
{
    public sealed class ScopeStack<T>
    {
        readonly Stack<Scope> _scopes = new();

        Scope? _global;

        public IReadOnlyCollection<Scope> Scopes => _scopes;

        public Scope Current =>
            _scopes.Count > 0
                ? _scopes.Peek()
                : throw new InvalidOperationException("No active scope.");

        public Scope Global =>
            _global ?? throw new InvalidOperationException("No global scope.");

        public bool HasScope => _scopes.Count > 0;

        public Scope Open()
        {
            Scope? parent = _scopes.Count > 0
                ? _scopes.Peek()
                : null;

            Scope scope = new(parent);

            _scopes.Push(scope);

            _global ??= scope;

            return scope;
        }

        public void Close()
        {
            if (_scopes.Count == 0)
                throw new InvalidOperationException("No scope to close.");

            Scope scope = _scopes.Pop();

            if (!ReferenceEquals(scope, _global))
                scope.Clear();
        }

        public void Clear()
        {
            while (_scopes.Count > 0)
                _scopes.Pop().Clear();

            _global?.Clear();
            _global = null;
        }

        public IDisposable Guard()
        {
            return new ScopeGuard(this);
        }

        public bool TryResolve(string name, out Scope scope)
        {
            for (Scope? current = Current; current is not null; current = current.Parent)
            {
                if (current.Contains(name))
                {
                    scope = current;
                    return true;
                }
            }

            scope = null!;
            return false;
        }

        public bool TryGet(string name, out T value)
        {
            if (TryResolve(name, out Scope scope))
            {
                value = scope[name];
                return true;
            }

            value = default!;
            return false;
        }

        public T Get(string name)
        {
            if (TryGet(name, out T value))
                return value;

            throw new InvalidOperationException($"Variable '{name}' is not defined.");
        }

        public void Set(string name, T value)
        {
            if (TryResolve(name, out Scope scope))
            {
                scope[name] = value;
                return;
            }

            Current[name] = value;
        }

        public T Declare(string name, T value)
        {
            return Current.Declare(name, value);
        }

        sealed class ScopeGuard : IDisposable
        {
            readonly ScopeStack<T> _stack;
            bool _disposed;

            public ScopeGuard(ScopeStack<T> stack)
            {
                _stack = stack;
                _stack.Open();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _stack.Close();
                _disposed = true;
            }
        }

        public sealed class Scope
        {
            readonly Dictionary<string, T> _variables = new(StringComparer.Ordinal);

            public Scope(Scope? parent)
            {
                Parent = parent;
            }

            public Scope? Parent { get; }

            public IReadOnlyDictionary<string, T> Variables => _variables;

            public void Clear()
            {
                _variables.Clear();
            }

            public bool Contains(string name)
            {
                return _variables.ContainsKey(name);
            }

            public bool TryGet(string name, out T value)
            {
                return _variables.TryGetValue(name, out value!);
            }

            public bool Shadows(string name)
            {
                for (Scope? scope = Parent; scope is not null; scope = scope.Parent)
                {
                    if (scope.Contains(name))
                        return true;
                }

                return false;
            }

            public T Declare(string name, T value)
            {
                if (Contains(name))
                    throw new InvalidOperationException(
                        $"Variable '{name}' is already declared in this scope.");

                _variables[name] = value;
                return value;
            }

            public T this[string name]
            {
                get => _variables[name];
                set => _variables[name] = value;
            }
        }
    }
}
