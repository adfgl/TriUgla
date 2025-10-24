using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public abstract class VisitorBase<T> : INodeVisitor<T>
    {
        protected VisitorBase(ScopeStack scope, Source source, Diagnostics diagnostics)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public ScopeStack Scope { get; }
        public Source Source { get; }
        public Diagnostics Diagnostics { get; }

        protected sealed class ScopeGuard : IDisposable
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

        protected IDisposable WithScope() => new ScopeGuard(Scope);

        protected void Report(string message, Token token)
            => Diagnostics.Report(Source, ESeverity.Error, message, token.position, token.span);

        protected void Report(string message, Token a, Token b)
            => Diagnostics.Report(Source, ESeverity.Error, message, a.position, TextSpan.Sum(a.span, b.span));

        protected Variable DeclareHere(string name)
        {
            var v = new Variable(name);
            Scope.Current.Declare(v);
            return v;
        }

        protected bool TryResolve(string name, out Variable variable)
        {
            // Walk from current outward using Scope.Parent chain.
            for (Scope? s = Scope.Current; s != null; s = s.Parent)
            {
                if (s.TryGet(name, out variable))
                    return true;
            }
            variable = null!;
            return false;
        }

        protected static bool TypesCompatible(EDataType left, EDataType right)
            => left == right || (left == EDataType.Real && right == EDataType.Integer);

        protected static string SymbolName(ExprLiteralSymbol sym) => sym.ToString();

        public abstract bool Visit(ExprAssignment node, out T result);
        public abstract bool Visit(ExprBinary node, out T result);
        public abstract bool Visit(ExprGroup node, out T result);
        public abstract bool Visit(ExprLiteralInteger node, out T result);
        public abstract bool Visit(ExprLiteralReal node, out T result);
        public abstract bool Visit(ExprLiteralString node, out T result);
        public abstract bool Visit(ExprLiteralSymbol node, out T result);
        public abstract bool Visit(ExprUnaryPrefix node, out T result);
        public abstract bool Visit(ExprUnaryPostfix node, out T result);
        public abstract bool Visit(ExprWithUnit node, out T result);

        public abstract bool Visit(StmtBlock node, out T result);
        public abstract bool Visit(StmtPrint node, out T result);
        public abstract bool Visit(StmtProgram node, out T result);
        public abstract bool Visit(StmtExpr node, out T result);
    }

}
