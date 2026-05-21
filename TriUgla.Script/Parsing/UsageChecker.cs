using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class UsageChecker(Diagnostics diagnostics) : INodeVisitor<object?>
    {
        sealed class Usage
        {
            public required Token Token;

            public bool Read;
            public bool Written;

            public bool IsParameter;
            public bool IsLoopVariable;
        }

        readonly ScopeStack<Usage> _scopes = new();
        readonly Diagnostics _diagnostics = diagnostics;

        public void Check(StmtProg program)
        {
            program.Accept(this);
        }

        public object? Visit(StmtProg node)
        {
            using (_scopes.Guard())
            {
                foreach (Stmt stmt in node.Statements)
                    stmt.Accept(this);

                FlushScope();
            }

            return null;
        }

        public object? Visit(StmtBlock node)
        {
            using (_scopes.Guard())
            {
                foreach (Stmt stmt in node.Statements)
                    stmt.Accept(this);

                FlushScope();
            }

            return null;
        }

        public object? Visit(StmtExpr node)
        {
            node.Expression.Accept(this);
            return null;
        }

        public object? Visit(StmtAssign node)
        {
            node.Value.Accept(this);

            switch (node.Target)
            {
                case ExprIdentifier id:

                    AssignIdentifier(id, node.Op);
                    break;

                case ExprIndex index:

                    index.Target.Accept(this);
                    index.Index?.Accept(this);

                    break;

                case ExprMember member:

                    member.Target.Accept(this);
                    break;

                default:

                    node.Target.Accept(this);
                    break;
            }

            return null;
        }

        void AssignIdentifier(ExprIdentifier id, Token op)
        {
            string name = id.Token.Text;

            bool compound =
                op.Is(OperatorKind.PlusAssign) ||
                op.Is(OperatorKind.MinusAssign) ||
                op.Is(OperatorKind.MultiplyAssign) ||
                op.Is(OperatorKind.DivideAssign);

            if (!_scopes.TryResolve(name, out var scope))
            {
                if (compound)
                {
                    _diagnostics.Error(
                        $"Variable '{name}' is used before assignment.",
                        id.Token);
                }

                Declare(name, id.Token);
                scope = _scopes.Current;
            }

            Usage usage = scope[name];

            if (compound)
                usage.Read = true;

            usage.Written = true;
        }

        public object? Visit(StmtIf node)
        {
            node.Condition.Accept(this);

            node.ThenBranch.Accept(this);

            foreach (StmtElseIf item in node.ElseIfs)
                item.Accept(this);

            node.ElseBranch?.Accept(this);

            return null;
        }

        public object? Visit(StmtElseIf node)
        {
            node.Condition.Accept(this);
            node.Body.Accept(this);
            return null;
        }

        public object? Visit(StmtWhile node)
        {
            node.Condition.Accept(this);
            node.Body.Accept(this);
            return null;
        }

        public object? Visit(StmtFor node)
        {
            node.Iterable.Accept(this);

            using (_scopes.Guard())
            {
                string name = node.Identifier.Text;

                Declare(name, node.Identifier);

                Usage usage = _scopes.Current[name];
                usage.Written = true;
                usage.IsLoopVariable = true;

                node.Body.Accept(this);

                FlushScope();
            }

            return null;
        }

        public object? Visit(StmtBreak node) => null;

        public object? Visit(StmtContinue node) => null;

        public object? Visit(StmtReturn node)
        {
            node.Value?.Accept(this);
            return null;
        }

        public object? Visit(StmtPoint node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtLine node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtCircle node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtEllipse node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtSpline node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtBSpline node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtBezier node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtCurveLoop node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtPlaneSurface node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            return null;
        }

        public object? Visit(StmtPhysical node)
        {
            node.Id.Accept(this);
            node.Values.Accept(this);
            node.Name?.Accept(this);

            return null;
        }

        public object? Visit(StmtTransfiniteCurve node)
        {
            node.Curves.Accept(this);
            node.Divisions.Accept(this);
            node.Progression.Accept(this);

            return null;
        }

        public object? Visit(StmtTransfiniteSurface node)
        {
            node.Surfaces.Accept(this);
            node.Corners?.Accept(this);

            return null;
        }

        public object? Visit(StmtRecombineSurface node)
        {
            node.Surfaces.Accept(this);
            return null;
        }

        public object? Visit(StmtEmbed node)
        {
            node.Entities.Accept(this);
            node.Containers.Accept(this);

            return null;
        }

        public object? Visit(StmtMeshOption node)
        {
            node.Value.Accept(this);
            return null;
        }

        public object? Visit(StmtMeshCommand node)
        {
            foreach (Expr arg in node.Args)
                arg.Accept(this);

            return null;
        }

        public object? Visit(StmtError node)
            => null;

        public object? Visit(ExprIdentifier node)
        {
            string name = node.Token.Text;

            if (!_scopes.TryResolve(name, out var scope))
            {
                _diagnostics.Error(
                    $"Variable '{name}' is used before assignment.",
                    node.Token);

                return null;
            }

            scope[name].Read = true;

            return null;
        }

        public object? Visit(ExprNumber node) => null;

        public object? Visit(ExprString node) => null;

        public object? Visit(ExprBoolean node) => null;

        public object? Visit(ExprGroup node)
        {
            node.Inner.Accept(this);
            return null;
        }

        public object? Visit(ExprUnary node)
        {
            node.Right.Accept(this);
            return null;
        }

        public object? Visit(ExprBinary node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);

            return null;
        }

        public object? Visit(ExprTernary node)
        {
            node.Condition.Accept(this);
            node.WhenTrue.Accept(this);
            node.WhenFalse.Accept(this);

            return null;
        }

        public object? Visit(ExprCall node)
        {
            if (node.Target is ExprIdentifier id &&
                Builtins.TryGet(id.Token.Text, out _))
            {
                foreach (Expr arg in node.Args)
                    arg.Accept(this);

                return null;
            }

            node.Target.Accept(this);

            foreach (Expr arg in node.Args)
                arg.Accept(this);

            return null;
        }

        public object? Visit(ExprList node)
        {
            foreach (Expr value in node.Values)
                value.Accept(this);

            return null;
        }

        public object? Visit(ExprRange node)
        {
            node.Start.Accept(this);
            node.End.Accept(this);
            node.Step?.Accept(this);

            return null;
        }

        public object? Visit(ExprIndex node)
        {
            node.Target.Accept(this);
            node.Index?.Accept(this);

            return null;
        }

        public object? Visit(ExprMember node)
        {
            node.Target.Accept(this);
            return null;
        }

        public object? Visit(ExprError node)
            => null;

        void Declare(string name, Token token)
        {
            if (_scopes.Current.Contains(name))
            {
                _diagnostics.Error(
                    $"Variable '{name}' is already declared in this scope.",
                    token);

                return;
            }

            if (_scopes.Current.Shadows(name))
            {
                _diagnostics.Warning(
                    $"Variable '{name}' shadows outer variable.",
                    token);
            }

            _scopes.Current.Declare(name, new Usage
            {
                Token = token
            });
        }

        void FlushScope()
        {
            foreach ((string name, Usage usage) in _scopes.Current.Variables)
            {
                if (!usage.Read && usage.Written)
                {
                    string kind =
                        usage.IsLoopVariable
                        ? "Loop variable"
                        : "Variable";

                    _diagnostics.Warning(
                        $"{kind} '{name}' is written but never read.",
                        usage.Token);
                }
            }
        }
    }
}
