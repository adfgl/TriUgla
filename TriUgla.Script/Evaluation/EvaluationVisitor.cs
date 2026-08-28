namespace TriUgla.Script;

public sealed class EvaluationVisitor : INodeVisitor<Value>
{
    readonly Scope _scope;
    readonly IReadOnlyDictionary<string, Func<IReadOnlyList<Value>, Value>> _functions;
    readonly List<Value> _printedValues = [];

    public EvaluationVisitor(
        Scope? scope = null,
        IReadOnlyDictionary<string, Func<IReadOnlyList<Value>, Value>>? functions = null)
    {
        _scope = scope ?? new Scope();
        var availableFunctions = new Dictionary<string, Func<IReadOnlyList<Value>, Value>>(
            StringComparer.Ordinal)
        {
            ["Print"] = Print
        };

        if (functions is not null)
        {
            foreach ((string name, Func<IReadOnlyList<Value>, Value> function) in functions)
            {
                if (!availableFunctions.TryAdd(name, function))
                {
                    throw new ArgumentException($"Function '{name}' is already registered.", nameof(functions));
                }
            }
        }

        _functions = availableFunctions;
    }

    public Scope Scope => _scope;
    public IReadOnlyList<Value> PrintedValues => _printedValues;
    public event Action<Value>? Printed;

    public Value Evaluate(AstNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Accept(this);
    }

    public Value VisitCompilationUnit(CompilationUnit node)
        => EvaluateStatements(node.Statements);

    public Value VisitNameExpression(NameExpr node)
        => _scope.TryGetValue(node.Name.Text, out Value value)
            ? value
            : throw new InvalidOperationException($"Variable '{node.Name.Text}' is not declared.");

    public Value VisitLiteralExpression(LiteralExpr node) => node.Value;

    public Value VisitErrorExpression(ErrorExpr node)
        => throw new InvalidOperationException("Cannot evaluate an erroneous expression.");

    public Value VisitUnaryExpression(UnaryExpr node)
    {
        double operand = Evaluate(node.Operand).Number;
        return node.Operator.Kind switch
        {
            TokenKind.Plus => operand,
            TokenKind.Minus => -operand,
            TokenKind.Bang => Boolean(!IsTruthy(operand)),
            _ => throw UnsupportedOperator(node.Operator)
        };
    }

    public Value VisitBinaryExpression(BinaryExpr node)
    {
        Value left = Evaluate(node.Left);
        Value right = Evaluate(node.Right);

        if (node.Operator.Kind == TokenKind.Plus &&
            left.ObjectOrNull() is ScriptString leftText &&
            right.ObjectOrNull() is ScriptString rightText)
        {
            return leftText.Value + rightText.Value;
        }

        if (node.Operator.Kind is TokenKind.EqualsEquals or TokenKind.BangEquals)
        {
            bool equal = ValuesEqual(left, right);
            return Boolean(node.Operator.Kind == TokenKind.EqualsEquals ? equal : !equal);
        }

        double leftNumber = left.Number;
        double rightNumber = right.Number;
        return node.Operator.Kind switch
        {
            TokenKind.Plus => leftNumber + rightNumber,
            TokenKind.Minus => leftNumber - rightNumber,
            TokenKind.Star => leftNumber * rightNumber,
            TokenKind.Slash => leftNumber / rightNumber,
            TokenKind.Percent => leftNumber % rightNumber,
            TokenKind.Less => Boolean(leftNumber < rightNumber),
            TokenKind.LessOrEquals => Boolean(leftNumber <= rightNumber),
            TokenKind.Greater => Boolean(leftNumber > rightNumber),
            TokenKind.GreaterOrEquals => Boolean(leftNumber >= rightNumber),
            _ => throw UnsupportedOperator(node.Operator)
        };
    }

    public Value VisitGroupExpression(GroupExpr node) => Evaluate(node.Expression);

    public Value VisitCallExpression(CallExpr node)
    {
        if (node.Callee is not NameExpr name || !_functions.TryGetValue(name.Name.Text, out var function))
        {
            throw new InvalidOperationException("Only registered named functions can be called.");
        }

        Value[] arguments = node.Arguments.Select(Evaluate).ToArray();
        return function(arguments);
    }

    public Value VisitListExpression(ListExpr node)
        => new ScriptList(node.Items.Select(Evaluate));

    public Value VisitExpressionStatement(ExpressionStmt node) => Evaluate(node.Expression);

    public Value VisitAssignmentStatement(AssignmentStmt node)
    {
        if (node.Target is not NameExpr name)
        {
            throw new InvalidOperationException("Assignment targets must be variable names.");
        }

        Value value = Evaluate(node.Value);
        if (!_scope.TryAssign(name.Name.Text, value))
        {
            _scope.Declare(name.Name.Text, value);
        }

        return value;
    }

    public Value VisitBlockStatement(BlockStmt node)
    {
        using (_scope.Open())
        {
            return EvaluateStatements(node.Statements);
        }
    }

    Value EvaluateStatements(IReadOnlyList<Stmt> statements)
    {
        Value result = 0d;
        foreach (Stmt statement in statements)
        {
            result = Evaluate(statement);
        }

        return result;
    }

    Value Print(IReadOnlyList<Value> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new InvalidOperationException(
                $"Print expects exactly one argument, but received {arguments.Count}.");
        }

        _printedValues.Add(arguments[0]);
        Printed?.Invoke(arguments[0]);
        return 0d;
    }

    static Value Boolean(bool value) => value ? 1d : 0d;
    static bool IsTruthy(double value) => value != 0d;

    static bool ValuesEqual(Value left, Value right)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (left.IsNumber)
        {
            return left.Number.Equals(right.Number);
        }

        return left.Object is ScriptString leftText && right.Object is ScriptString rightText
            ? StringComparer.Ordinal.Equals(leftText.Value, rightText.Value)
            : Equals(left.Object, right.Object);
    }

    static InvalidOperationException UnsupportedOperator(Token token)
        => new($"Operator '{token.Text}' cannot be evaluated.");
}

static class EvaluationValueExtensions
{
    public static ScriptObject? ObjectOrNull(this Value value)
        => value.IsObject ? value.Object : null;
}
