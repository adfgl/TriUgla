namespace TriUgla.Script;

public sealed class EvaluationVisitor : INodeVisitor<Value>
{
    const int MaximumLoopIterations = 1_000_000;
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

        if (left.ObjectOrNull() is ScriptList || right.ObjectOrNull() is ScriptList)
        {
            return EvaluateListOperation(left, node.Operator, right);
        }

        if (!left.IsNumber || !right.IsNumber)
        {
            throw UnsupportedOperands(
                node.Operator,
                left,
                right,
                "Use two numbers, two numeric lists, a numeric list and a scalar, or '+' with two strings.");
        }

        double leftNumber = left.Number;
        double rightNumber = right.Number;
        if (node.Operator.Kind == TokenKind.Slash && rightNumber == 0d)
        {
            throw new InvalidOperationException(
                "Operator '/' cannot divide by zero. Hint: make the right operand a non-zero number.");
        }

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

    static Value EvaluateListOperation(Value left, Token operation, Value right)
    {
        if (operation.Kind is not TokenKind.Plus and not TokenKind.Star and not TokenKind.Slash)
        {
            throw new InvalidOperationException(
                $"Operator '{operation.Text}' cannot be applied when an operand is a list. " +
                "Hint: use '+', '*' or '/', or reduce the list to a scalar before applying this operator.");
        }

        ScriptList? leftList = left.ObjectOrNull() as ScriptList;
        ScriptList? rightList = right.ObjectOrNull() as ScriptList;
        if (leftList is not null && rightList is not null && leftList.Items.Count != rightList.Items.Count)
        {
            throw new InvalidOperationException(
                $"Cannot apply '{operation.Text}' to lists with different lengths " +
                $"({leftList.Items.Count} and {rightList.Items.Count}). " +
                "Hint: make both lists the same length, or replace one list with a scalar value.");
        }

        int count = leftList?.Items.Count ?? rightList!.Items.Count;
        var values = new Value[count];
        for (int index = 0; index < count; index++)
        {
            double leftNumber = ListOperandNumber(left, leftList, index, "left");
            double rightNumber = ListOperandNumber(right, rightList, index, "right");
            if (operation.Kind == TokenKind.Slash && rightNumber == 0d)
            {
                throw new InvalidOperationException(
                    $"Operator '/' cannot divide by zero at list index {index}. " +
                    "Hint: replace the corresponding right-hand value with a non-zero number.");
            }

            values[index] = operation.Kind switch
            {
                TokenKind.Plus => leftNumber + rightNumber,
                TokenKind.Star => leftNumber * rightNumber,
                TokenKind.Slash => leftNumber / rightNumber,
                _ => throw UnsupportedOperator(operation)
            };
        }

        return new ScriptList(values);
    }

    static double ListOperandNumber(
        Value operand,
        ScriptList? list,
        int index,
        string side)
    {
        Value item = list is null ? operand : list.Items[index];
        if (!item.IsNumber)
        {
            string location = list is null ? side + " scalar" : $"{side} list at index {index}";
            throw new InvalidOperationException(
                $"List arithmetic requires numeric values, but the {location} is {Describe(item)}. " +
                "Hint: replace that value with a number before applying the operation.");
        }

        return item.Number;
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

    public Value VisitIfStatement(IfStmt node)
    {
        foreach (ConditionalBranch branch in node.Branches)
        {
            if (branch.Condition is null || Evaluate(branch.Condition).Number != 0d)
            {
                return EvaluateStatements(branch.Statements);
            }
        }

        return 0d;
    }

    public Value VisitForStatement(ForStmt node)
    {
        double start = Evaluate(node.Start).Number;
        double end = Evaluate(node.End).Number;
        double step = node.Step is null ? 1d : Evaluate(node.Step).Number;
        if (step == 0d || double.IsNaN(step))
        {
            throw new InvalidOperationException("Loop step must be a non-zero number.");
        }

        Value result = 0d;
        int iterations = 0;
        using (_scope.Open())
        {
            if (node.Iterator is Token iterator)
            {
                _scope.Declare(iterator.Text, start);
            }

            for (double current = start;
                 step > 0d ? current <= end : current >= end;
                 current += step)
            {
                if (++iterations > MaximumLoopIterations)
                {
                    throw new InvalidOperationException(
                        $"Loop exceeded the limit of {MaximumLoopIterations} iterations.");
                }

                if (node.Iterator is Token currentIterator)
                {
                    _scope.TryAssign(currentIterator.Text, current);
                }

                result = EvaluateStatements(node.Statements);
            }
        }

        return result;
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

    static InvalidOperationException UnsupportedOperands(
        Token operation,
        Value left,
        Value right,
        string hint)
        => new(
            $"Operator '{operation.Text}' cannot be applied to {Describe(left)} and {Describe(right)}. " +
            $"Hint: {hint}");

    static string Describe(Value value)
        => value.IsNumber
            ? "a number"
            : value.Object switch
            {
                ScriptString => "a string",
                ScriptList => "a list",
                _ => $"an object of type {value.Object.GetType().Name}"
            };
}

static class EvaluationValueExtensions
{
    public static ScriptObject? ObjectOrNull(this Value value)
        => value.IsObject ? value.Object : null;
}
