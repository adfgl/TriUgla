namespace TriUgla.Script;

public sealed class SemanticAnalyzer
{
    readonly DiagnosticBag _diagnostics = new();
    readonly Stack<Dictionary<string, VariableDeclaration>> _scopes = new();

    public IReadOnlyList<Diagnostic> Analyze(CompilationUnit root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _diagnostics.Clear();
        _scopes.Clear();
        _scopes.Push(new Dictionary<string, VariableDeclaration>(StringComparer.Ordinal));
        AnalyzeStatements(root.Statements);
        CloseScope();
        return _diagnostics.Items;
    }

    void AnalyzeStatements(IEnumerable<Stmt> statements)
    {
        foreach (Stmt statement in statements)
        {
            AnalyzeStatement(statement);
        }
    }

    void AnalyzeStatement(Stmt statement)
    {
        switch (statement)
        {
            case AssignmentStmt assignment:
                AnalyzeExpression(assignment.Value);
                if (assignment.Target is NameExpr name)
                {
                    if (Find(name.Name.Text) is null)
                    {
                        _scopes.Peek().Add(name.Name.Text, new VariableDeclaration(name.Name));
                    }
                }
                else
                {
                    AnalyzeExpression(assignment.Target);
                }
                break;

            case ExpressionStmt expression:
                AnalyzeExpression(expression.Expression);
                break;

            case BlockStmt block:
                OpenScope();
                AnalyzeStatements(block.Statements);
                CloseScope();
                break;

            case IfStmt conditional:
                foreach (ConditionalBranch branch in conditional.Branches)
                {
                    if (branch.Condition is not null) AnalyzeExpression(branch.Condition);
                    AnalyzeStatements(branch.Statements);
                }
                break;

            case ForStmt loop:
                if (loop.Start is not null) AnalyzeExpression(loop.Start);
                if (loop.End is not null) AnalyzeExpression(loop.End);
                if (loop.Step is not null) AnalyzeExpression(loop.Step);
                if (loop.Items is not null)
                {
                    foreach (Expr item in loop.Items) AnalyzeExpression(item);
                }
                OpenScope();
                if (loop.Iterator is Token iterator)
                {
                    _scopes.Peek().Add(iterator.Text, new VariableDeclaration(iterator));
                }
                AnalyzeStatements(loop.Statements);
                CloseScope();
                break;

            case TransfiniteCurveStmt transfinite:
                foreach (Expr curve in transfinite.Curves) AnalyzeExpression(curve);
                AnalyzeExpression(transfinite.NodeCount);
                if (transfinite.Coefficient is not null) AnalyzeExpression(transfinite.Coefficient);
                break;

            case CurveLoopStmt loop:
                AnalyzeExpression(loop.Tag);
                AnalyzeExpression(loop.Curves);
                break;

            case PlaneSurfaceStmt surface:
                AnalyzeExpression(surface.Tag);
                AnalyzeExpression(surface.CurveLoops);
                break;

            case CurvesInSurfaceStmt embedded:
                AnalyzeExpression(embedded.Curves);
                AnalyzeExpression(embedded.Surfaces);
                break;

            case MeshCommandStmt meshCommand when meshCommand.Dimension is not null:
                AnalyzeExpression(meshCommand.Dimension);
                break;

            case PhysicalPointStmt physicalPoint:
                AnalyzeExpression(physicalPoint.Name);
                AnalyzeExpression(physicalPoint.Points);
                break;
        }
    }

    void AnalyzeExpression(Expr expression)
    {
        switch (expression)
        {
            case NameExpr name:
                VariableDeclaration? declaration = Find(name.Name.Text);
                if (declaration is not null) declaration.Used = true;
                break;
            case UnaryExpr unary:
                AnalyzeExpression(unary.Operand);
                break;
            case BinaryExpr binary:
                AnalyzeExpression(binary.Left);
                AnalyzeExpression(binary.Right);
                break;
            case GroupExpr group:
                AnalyzeExpression(group.Expression);
                break;
            case CallExpr call:
                if (call.Callee is not NameExpr) AnalyzeExpression(call.Callee);
                foreach (Expr argument in call.Arguments) AnalyzeExpression(argument);
                break;
            case ListExpr list:
                foreach (Expr item in list.Items) AnalyzeExpression(item);
                break;
            case IndexExpr index:
                AnalyzeExpression(index.Target);
                AnalyzeExpression(index.Index);
                break;
            case MemberAccessExpr member:
                AnalyzeExpression(member.Target);
                break;
        }
    }

    VariableDeclaration? Find(string name)
    {
        foreach (Dictionary<string, VariableDeclaration> scope in _scopes)
        {
            if (scope.TryGetValue(name, out VariableDeclaration? declaration)) return declaration;
        }
        return null;
    }

    void OpenScope() => _scopes.Push(new Dictionary<string, VariableDeclaration>(StringComparer.Ordinal));

    void CloseScope()
    {
        foreach (VariableDeclaration declaration in _scopes.Pop().Values.Where(item => !item.Used))
        {
            _diagnostics.Warning(
                "TS2001",
                $"Variable '{declaration.Token.Text}' is declared but never used.",
                declaration.Token.Span);
        }
    }

    sealed class VariableDeclaration(Token token)
    {
        public Token Token { get; } = token;
        public bool Used { get; set; }
    }
}
