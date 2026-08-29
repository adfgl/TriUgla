namespace TriUgla.Script;

public sealed class EvaluationVisitor : INodeVisitor<Value>
{
    const int MaximumLoopIterations = 1_000_000;
    readonly Scope _scope;
    readonly GeometryModel _geometry;
    readonly MeshScriptModel _mesh;
    readonly IReadOnlyDictionary<string, Func<IReadOnlyList<Value>, Value>> _functions;
    readonly List<Value> _printedValues = [];

    public EvaluationVisitor(
        Scope? scope = null,
        IReadOnlyDictionary<string, Func<IReadOnlyList<Value>, Value>>? functions = null,
        GeometryModel? geometry = null,
        MeshScriptModel? mesh = null)
    {
        _scope = scope ?? new Scope();
        _geometry = geometry ?? new GeometryModel();
        _mesh = mesh ?? new MeshScriptModel();
        if (!_scope.Declare("Mesh", _mesh))
        {
            throw new ArgumentException("The supplied scope already declares the built-in object 'Mesh'.", nameof(scope));
        }
        var availableFunctions = new Dictionary<string, Func<IReadOnlyList<Value>, Value>>(
            StringComparer.Ordinal)
        {
            ["Print"] = Print
        };

        foreach ((string name, Func<IReadOnlyList<Value>, Value> function) in StandardMathFunctions.All)
        {
            availableFunctions.Add(name, function);
        }

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
    public GeometryModel Geometry => _geometry;
    public MeshScriptModel Mesh => _mesh;
    public IReadOnlyList<Value> PrintedValues => _printedValues;
    public event Action<Value>? Printed;

    public Value Evaluate(AstNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Accept(this);
    }

    public async ValueTask<Value> EvaluateAsync(
        AstNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        cancellationToken.ThrowIfCancellationRequested();
        return node switch
        {
            CompilationUnit compilation => await EvaluateStatementsAsync(
                compilation.Statements,
                cancellationToken),
            BlockStmt block => await EvaluateBlockAsync(block, cancellationToken),
            IfStmt conditional => await EvaluateIfAsync(conditional, cancellationToken),
            ForStmt loop => await EvaluateForAsync(loop, cancellationToken),
            _ => Evaluate(node)
        };
    }

    async ValueTask<Value> EvaluateBlockAsync(BlockStmt node, CancellationToken cancellationToken)
    {
        using (_scope.Open())
        {
            return await EvaluateStatementsAsync(node.Statements, cancellationToken);
        }
    }

    async ValueTask<Value> EvaluateIfAsync(IfStmt node, CancellationToken cancellationToken)
    {
        foreach (ConditionalBranch branch in node.Branches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (branch.Condition is null || Evaluate(branch.Condition).Number != 0d)
            {
                return await EvaluateStatementsAsync(branch.Statements, cancellationToken);
            }
        }

        return 0d;
    }

    async ValueTask<Value> EvaluateForAsync(ForStmt node, CancellationToken cancellationToken)
    {
        if (node.Items is not null)
        {
            return await EvaluateExplicitLoopAsync(node, cancellationToken);
        }

        double start = Evaluate(node.Start!).Number;
        double end = Evaluate(node.End!).Number;
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
                cancellationToken.ThrowIfCancellationRequested();
                if (++iterations > MaximumLoopIterations)
                {
                    throw new InvalidOperationException(
                        $"Loop exceeded the limit of {MaximumLoopIterations} iterations.");
                }

                if (node.Iterator is Token currentIterator)
                {
                    _scope.TryAssign(currentIterator.Text, current);
                }

                result = await EvaluateStatementsAsync(node.Statements, cancellationToken);
                if ((iterations & 127) == 0)
                {
                    await Task.Yield();
                }
            }
        }

        return result;
    }

    async ValueTask<Value> EvaluateExplicitLoopAsync(ForStmt node, CancellationToken cancellationToken)
    {
        Value result = 0d;
        int iterations = 0;
        using (_scope.Open())
        {
            if (node.Iterator is not Token iterator)
            {
                throw new InvalidOperationException(
                    "An explicit value loop requires an iterator name. Hint: use 'For item In { ... }'.");
            }

            _scope.Declare(iterator.Text, 0d);
            foreach (Expr item in node.Items!)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++iterations > MaximumLoopIterations)
                {
                    throw new InvalidOperationException(
                        $"Loop exceeded the limit of {MaximumLoopIterations} iterations.");
                }

                _scope.TryAssign(iterator.Text, Evaluate(item));
                result = await EvaluateStatementsAsync(node.Statements, cancellationToken);
                if ((iterations & 127) == 0)
                {
                    await Task.Yield();
                }
            }
        }

        return result;
    }

    async ValueTask<Value> EvaluateStatementsAsync(
        IReadOnlyList<Stmt> statements,
        CancellationToken cancellationToken)
    {
        Value result = 0d;
        foreach (Stmt statement in statements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = await EvaluateAsync(statement, cancellationToken);
        }

        return result;
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
        if (IsMatrixLike(leftList) || IsMatrixLike(rightList))
        {
            return EvaluateMatrixOperation(left, leftList, operation, right, rightList);
        }

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

    static Value EvaluateMatrixOperation(
        Value left,
        ScriptList? leftList,
        Token operation,
        Value right,
        ScriptList? rightList)
    {
        double[][]? leftMatrix = IsMatrixLike(leftList) ? ReadMatrix(leftList!, "left") : null;
        double[][]? rightMatrix = IsMatrixLike(rightList) ? ReadMatrix(rightList!, "right") : null;
        double[]? leftVector = leftList is not null && leftMatrix is null ? ReadVector(leftList, "left") : null;
        double[]? rightVector = rightList is not null && rightMatrix is null ? ReadVector(rightList, "right") : null;

        if (operation.Kind == TokenKind.Star)
        {
            if (leftMatrix is not null && rightVector is not null)
            {
                RequireDimensions(
                    leftMatrix[0].Length == rightVector.Length,
                    $"matrix columns ({leftMatrix[0].Length})",
                    $"vector length ({rightVector.Length})");
                return VectorValue(leftMatrix.Select(row => Dot(row, rightVector)));
            }

            if (leftVector is not null && rightMatrix is not null)
            {
                RequireDimensions(
                    leftVector.Length == rightMatrix.Length,
                    $"vector length ({leftVector.Length})",
                    $"matrix rows ({rightMatrix.Length})");
                return VectorValue(Enumerable.Range(0, rightMatrix[0].Length)
                    .Select(column => Dot(leftVector, rightMatrix.Select(row => row[column]).ToArray())));
            }

            if (leftMatrix is not null && rightMatrix is not null)
            {
                RequireDimensions(
                    leftMatrix[0].Length == rightMatrix.Length,
                    $"left matrix columns ({leftMatrix[0].Length})",
                    $"right matrix rows ({rightMatrix.Length})");
                return MatrixValue(leftMatrix.Select(row =>
                    Enumerable.Range(0, rightMatrix[0].Length)
                        .Select(column => Dot(row, rightMatrix.Select(item => item[column]).ToArray()))));
            }
        }

        if (leftMatrix is not null && rightList is null)
        {
            return MatrixScalar(leftMatrix, RequireScalar(right, "right"), operation, scalarOnLeft: false);
        }

        if (leftList is null && rightMatrix is not null)
        {
            return MatrixScalar(rightMatrix, RequireScalar(left, "left"), operation, scalarOnLeft: true);
        }

        if (leftMatrix is not null && rightMatrix is not null &&
            operation.Kind is TokenKind.Plus or TokenKind.Slash)
        {
            RequireSameMatrixShape(leftMatrix, rightMatrix, operation.Text);
            return MatrixValue(leftMatrix.Select((row, rowIndex) =>
                row.Select((value, columnIndex) => ApplyMatrixCell(
                    value,
                    rightMatrix[rowIndex][columnIndex],
                    operation,
                    rowIndex,
                    columnIndex))));
        }

        throw new InvalidOperationException(
            $"Operator '{operation.Text}' cannot be applied to these vector and matrix shapes. " +
            "Hint: for '*', match the left columns/length to the right rows/length; " +
            "for '+' or '/', use matrices with identical dimensions or a matrix and a scalar.");
    }

    static bool IsMatrixLike(ScriptList? list)
        => list is not null && list.Items.Any(item => item.ObjectOrNull() is ScriptList);

    static double[] ReadVector(ScriptList list, string side)
        => list.Items.Select((item, index) =>
        {
            if (!item.IsNumber)
            {
                throw new InvalidOperationException(
                    $"The {side} vector contains a non-numeric value at index {index}. " +
                    "Hint: vectors must contain only numbers.");
            }

            return item.Number;
        }).ToArray();

    static double[][] ReadMatrix(ScriptList list, string side)
    {
        if (list.Items.Count == 0)
        {
            throw new InvalidOperationException(
                $"The {side} matrix is empty. Hint: provide at least one non-empty numeric row.");
        }

        var rows = new double[list.Items.Count][];
        int? columns = null;
        for (int rowIndex = 0; rowIndex < list.Items.Count; rowIndex++)
        {
            if (list.Items[rowIndex].ObjectOrNull() is not ScriptList row)
            {
                throw new InvalidOperationException(
                    $"The {side} matrix mixes rows and scalar values at row {rowIndex}. " +
                    "Hint: wrap every matrix row in braces.");
            }

            rows[rowIndex] = ReadVector(row, $"{side} matrix row {rowIndex}");
            if (rows[rowIndex].Length == 0)
            {
                throw new InvalidOperationException(
                    $"The {side} matrix row {rowIndex} is empty. Hint: every row must contain numeric values.");
            }

            columns ??= rows[rowIndex].Length;
            if (rows[rowIndex].Length != columns)
            {
                throw new InvalidOperationException(
                    $"The {side} matrix is ragged: row 0 has {columns} columns but row {rowIndex} " +
                    $"has {rows[rowIndex].Length}. Hint: make every matrix row the same length.");
            }
        }

        return rows;
    }

    static Value MatrixScalar(double[][] matrix, double scalar, Token operation, bool scalarOnLeft)
        => MatrixValue(matrix.Select((row, rowIndex) =>
            row.Select((value, columnIndex) => ApplyMatrixCell(
                scalarOnLeft ? scalar : value,
                scalarOnLeft ? value : scalar,
                operation,
                rowIndex,
                columnIndex))));

    static double ApplyMatrixCell(
        double left,
        double right,
        Token operation,
        int row,
        int column)
    {
        if (operation.Kind == TokenKind.Slash && right == 0d)
        {
            throw new InvalidOperationException(
                $"Operator '/' cannot divide by zero at matrix row {row}, column {column}. " +
                "Hint: replace the corresponding right-hand value with a non-zero number.");
        }

        return operation.Kind switch
        {
            TokenKind.Plus => left + right,
            TokenKind.Star => left * right,
            TokenKind.Slash => left / right,
            _ => throw new InvalidOperationException(
                $"Operator '{operation.Text}' is not supported for matrices. Hint: use '+', '*' or '/'.")
        };
    }

    static double RequireScalar(Value value, string side)
    {
        if (!value.IsNumber)
        {
            throw new InvalidOperationException(
                $"The {side} matrix operand must be a number. Hint: use a numeric scalar value.");
        }

        return value.Number;
    }

    static void RequireDimensions(bool valid, string left, string right)
    {
        if (!valid)
        {
            throw new InvalidOperationException(
                $"Cannot multiply because {left} do not match {right}. " +
                "Hint: the inner dimensions of a multiplication must be equal.");
        }
    }

    static void RequireSameMatrixShape(double[][] left, double[][] right, string operation)
    {
        if (left.Length != right.Length || left[0].Length != right[0].Length)
        {
            throw new InvalidOperationException(
                $"Cannot apply '{operation}' to matrices sized " +
                $"{left.Length}x{left[0].Length} and {right.Length}x{right[0].Length}. " +
                "Hint: element-wise matrix operations require identical dimensions.");
        }
    }

    static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right)
        => left.Select((value, index) => value * right[index]).Sum();

    static Value VectorValue(IEnumerable<double> values)
        => new ScriptList(values.Select(value => new Value(value)));

    static Value MatrixValue(IEnumerable<IEnumerable<double>> rows)
        => new ScriptList(rows.Select(row => new Value(
            new ScriptList(row.Select(value => new Value(value))))));

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
        if (node.Callee is NameExpr primitiveName &&
            primitiveName.Name.Text is "Point" or "Line" or "Spline" or "BSpline" or "Bezier" or "Circle")
        {
            return ResolvePrimitive(node, primitiveName.Name.Text);
        }

        if (node.Callee is not NameExpr name || !_functions.TryGetValue(name.Name.Text, out var function))
        {
            throw new InvalidOperationException("Only registered named functions can be called.");
        }

        Value[] arguments = node.Arguments.Select(Evaluate).ToArray();
        return function(arguments);
    }

    Value ResolvePrimitive(CallExpr call, string primitiveName)
    {
        if (call.Arguments.Count != 1)
        {
            throw new InvalidOperationException(
                $"{primitiveName} lookup expects exactly one tag, but received {call.Arguments.Count}. " +
                $"Hint: use {primitiveName}(tag), for example {primitiveName}(1).");
        }

        int tag = RequireEntityTag(Evaluate(call.Arguments[0]), $"{primitiveName} lookup tag");
        ScriptObject? primitive = primitiveName == "Point"
            ? _geometry.Points.GetValueOrDefault(tag)
            : _geometry.Curves.GetValueOrDefault(tag);
        if (primitive is null)
        {
            throw new InvalidOperationException(
                $"{primitiveName}({tag}) is not registered. " +
                $"Hint: declare {primitiveName}({tag}) before using it.");
        }

        return primitive;
    }

    public Value VisitListExpression(ListExpr node)
        => new ScriptList(node.Items.Select(Evaluate));

    public Value VisitIndexExpression(IndexExpr node)
    {
        Value target = Evaluate(node.Target);
        if (target.ObjectOrNull() is not ScriptList list)
        {
            throw new InvalidOperationException(
                $"Index access cannot be applied to {Describe(target)}. " +
                "Hint: use square brackets only after a list or matrix expression.");
        }

        Value index = Evaluate(node.Index);
        if (index.ObjectOrNull() is ScriptList indices)
        {
            return new ScriptList(indices.Items.Select((item, position) =>
                list.Items[RequireListIndex(item, list.Items.Count, $"index list position {position}")]));
        }

        return list.Items[RequireListIndex(index, list.Items.Count, "list index")];
    }

    public Value VisitMemberAccessExpression(MemberAccessExpr node)
    {
        Value target = Evaluate(node.Target);
        if (!target.IsObject)
        {
            throw new InvalidOperationException(
                $"Cannot read property '{node.Member.Text}' from a non-object value.");
        }

        return target.Object.GetProperty(node.Member.Text);
    }

    public Value VisitExpressionStatement(ExpressionStmt node) => Evaluate(node.Expression);

    public Value VisitAssignmentStatement(AssignmentStmt node)
    {
        if (node.Target is CallExpr primitive)
        {
            return EvaluatePrimitiveDeclaration(primitive, node.Value);
        }


        if (node.Target is MemberAccessExpr member)
        {
            Value target = Evaluate(member.Target);
            if (!target.IsObject)
            {
                throw new InvalidOperationException(
                    $"Cannot assign property '{member.Member.Text}' on a non-object value.");
            }

            Value memberValue = Evaluate(node.Value);
            target.Object.SetProperty(member.Member.Text, memberValue);
            return memberValue;
        }

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

    public Value VisitMeshCommandStatement(MeshCommandStmt node)
    {
        MeshScriptCommandKind kind = node.Command.Text switch
        {
            "Mesh" => MeshScriptCommandKind.Generate,
            "Coherence" => MeshScriptCommandKind.Coherence,
            "RenumberMeshNodes" => MeshScriptCommandKind.RenumberNodes,
            "RenumberMeshElements" => MeshScriptCommandKind.RenumberElements,
            _ => throw new InvalidOperationException($"Unsupported mesh command '{node.Command.Text}'.")
        };

        int? dimension = null;
        if (node.Dimension is not null)
        {
            double value = Evaluate(node.Dimension).Number;
            if (!double.IsFinite(value) || value != Math.Truncate(value) || value is < 1 or > 3)
            {
                throw new InvalidOperationException("Mesh dimension must be an integer from 1 through 3.");
            }

            dimension = (int)value;
        }

        _mesh.AddCommand(kind, dimension);
        return dimension ?? 0d;
    }

    Value EvaluatePrimitiveDeclaration(CallExpr target, Expr valueExpression)
    {
        if (target.Callee is not NameExpr name)
        {
            throw new InvalidOperationException(
                "Primitive declaration target is invalid. Hint: use a supported point or curve primitive.");
        }

        string primitiveName = name.Name.Text;
        if (primitiveName is not "Point" and not "Line" and not "Spline" and not "BSpline" and not "Bezier" and not "Circle")
        {
            throw new InvalidOperationException(
                $"Assignment to function call '{primitiveName}(...)' is not supported. " +
                "Hint: assign to a variable or a supported geometry primitive.");
        }

        if (target.Arguments.Count != 1)
        {
            throw new InvalidOperationException(
                $"{primitiveName} expects exactly one tag inside parentheses, but received {target.Arguments.Count}. " +
                $"Hint: use {primitiveName}(1) = {{...}}.");
        }

        int tag = RequireEntityTag(Evaluate(target.Arguments[0]), $"{primitiveName} tag");
        Value value = Evaluate(valueExpression);
        if (value.ObjectOrNull() is not ScriptList parameters)
        {
            throw new InvalidOperationException(
                $"{primitiveName} {tag} requires a list on the right-hand side. " +
                $"Hint: use {primitiveName}({tag}) = {{...}}.");
        }

        return primitiveName switch
        {
            "Point" => CreatePoint(tag, parameters),
            "Line" => CreateLine(tag, parameters),
            "Spline" => CreateCurve(tag, parameters, ScriptCurveKind.Spline),
            "BSpline" => CreateCurve(tag, parameters, ScriptCurveKind.BSpline),
            "Bezier" => CreateCurve(tag, parameters, ScriptCurveKind.Bezier),
            "Circle" => CreateCurve(tag, parameters, ScriptCurveKind.Circle),
            _ => throw new InvalidOperationException($"Unsupported geometry primitive '{primitiveName}'.")
        };
    }

    Value CreatePoint(int tag, ScriptList parameters)
    {
        if (parameters.Items.Count is not 3 and not 4)
        {
            throw new InvalidOperationException(
                $"Point {tag} expects 3 coordinates and an optional mesh size, but received " +
                $"{parameters.Items.Count} values. Hint: use Point({tag}) = {{x, y, z}} or {{x, y, z, meshSize}}.");
        }

        double[] values = parameters.Items
            .Select((value, index) => RequirePrimitiveNumber("Point", tag, value, index))
            .ToArray();
        return _geometry.AddPoint(
            tag,
            values[0],
            values[1],
            values[2],
            values.Length == 4 ? values[3] : null);
    }

    Value CreateLine(int tag, ScriptList parameters)
    {
        if (parameters.Items.Count != 2)
        {
            throw new InvalidOperationException(
                $"Line {tag} expects exactly 2 point tags, but received {parameters.Items.Count}. " +
                $"Hint: use Line({tag}) = {{startPointTag, endPointTag}}.");
        }

        int start = RequireEntityTag(parameters.Items[0], $"Line {tag} start point");
        int end = RequireEntityTag(parameters.Items[1], $"Line {tag} end point");
        return _geometry.AddLine(tag, start, end);
    }

    Value CreateCurve(int tag, ScriptList parameters, ScriptCurveKind kind)
    {
        int minimum = kind == ScriptCurveKind.Circle ? 3 : 2;
        if (parameters.Items.Count < minimum ||
            kind == ScriptCurveKind.Circle && parameters.Items.Count != 3)
        {
            string expected = kind == ScriptCurveKind.Circle ? "exactly 3" : $"at least {minimum}";
            throw new InvalidOperationException(
                $"{kind} {tag} expects {expected} point tags, but received {parameters.Items.Count}. " +
                $"Hint: use {kind}({tag}) = {{pointTag, ...}}.");
        }

        int[] pointTags = parameters.Items
            .Select((value, index) => RequireEntityTag(value, $"{kind} {tag} point at index {index}"))
            .ToArray();
        return _geometry.AddSpline(tag, pointTags, kind);
    }

    static double RequirePrimitiveNumber(string primitive, int tag, Value value, int index)
    {
        if (!value.IsNumber || !double.IsFinite(value.Number))
        {
            throw new InvalidOperationException(
                $"{primitive} {tag} value at index {index} must be a finite number. " +
                "Hint: replace it with a numeric expression.");
        }

        return value.Number;
    }

    static int RequireEntityTag(Value value, string description)
    {
        if (!value.IsNumber || !double.IsFinite(value.Number) ||
            value.Number <= 0d || value.Number != Math.Truncate(value.Number) ||
            value.Number > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"{description} must be a positive whole number. Hint: use an integer tag such as 1.");
        }

        return (int)value.Number;
    }

    static int RequireListIndex(Value value, int count, string description)
    {
        if (!value.IsNumber || !double.IsFinite(value.Number) ||
            value.Number != Math.Truncate(value.Number))
        {
            throw new InvalidOperationException(
                $"The {description} must be a whole number, but received {Describe(value)}. " +
                "Hint: use a zero-based integer index such as 0.");
        }

        int index = value.Number is >= int.MinValue and <= int.MaxValue
            ? (int)value.Number
            : int.MinValue;
        if (index < 0 || index >= count)
        {
            string available = count == 0 ? "the list is empty" : $"valid indices are 0 through {count - 1}";
            throw new InvalidOperationException(
                $"List index {value.Number} is out of range; {available}. " +
                "Hint: choose an index within the list bounds.");
        }

        return index;
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
        if (node.Items is not null)
        {
            return EvaluateExplicitLoop(node);
        }

        double start = Evaluate(node.Start!).Number;
        double end = Evaluate(node.End!).Number;
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

    public Value VisitTransfiniteCurveStatement(TransfiniteCurveStmt node)
    {
        bool selectsAll = node.Curves.Count == 1 &&
            node.Curves[0] is NameExpr { Name.Text: "All" };
        if (!selectsAll && node.Curves.Any(curve => curve is NameExpr { Name.Text: "All" }))
        {
            throw new InvalidOperationException(
                "Transfinite Curve selector 'All' cannot be combined with explicit curve tags.");
        }

        int[] curves = selectsAll
            ? _geometry.Curves.Keys.Order().ToArray()
            : node.Curves
                .Select((curve, index) => RequireOrientedCurveTag(
                    Evaluate(curve),
                    $"Transfinite Curve tag at index {index}"))
                .ToArray();
        int nodeCount = RequireNodeCount(Evaluate(node.NodeCount));
        TransfiniteCurveDistribution distribution = node.DistributionKeyword?.Text switch
        {
            null or "Progression" => TransfiniteCurveDistribution.Progression,
            "Bump" => TransfiniteCurveDistribution.Bump,
            string option => throw new InvalidOperationException(
                $"Unsupported Transfinite Curve distribution '{option}'. " +
                "Hint: use 'Using Progression value' or 'Using Bump value'.")
        };
        double coefficient = node.Coefficient is null
            ? 1d
            : RequireTransfiniteCoefficient(Evaluate(node.Coefficient), distribution);
        return _geometry.SetTransfiniteCurves(curves, nodeCount, distribution, coefficient);
    }

    public Value VisitPhysicalPointStatement(PhysicalPointStmt node)
    {
        Value nameValue = Evaluate(node.Name);
        if (nameValue.ObjectOrNull() is not ScriptString name)
        {
            throw new InvalidOperationException("Physical Point expects a string name, for example Physical Point(\"Support\") = {1};");
        }

        int[] tags = node.Points.Items
            .Select((point, index) => RequireEntityTag(Evaluate(point), $"Physical Point tag at index {index}"))
            .ToArray();
        return _geometry.AddPhysicalPointGroup(name.Value, tags);
    }

    public Value VisitCurveLoopStatement(CurveLoopStmt node)
    {
        int tag = RequireEntityTag(Evaluate(node.Tag), "Curve Loop tag");
        int[] curves = node.Curves.Items
            .Select((curve, index) => RequireOrientedCurveTag(
                Evaluate(curve),
                $"Curve Loop {tag} curve at index {index}"))
            .ToArray();
        return _geometry.AddCurveLoop(tag, curves);
    }

    public Value VisitPlaneSurfaceStatement(PlaneSurfaceStmt node)
    {
        int tag = RequireEntityTag(Evaluate(node.Tag), "Plane Surface tag");
        int[] loopTags = node.CurveLoops.Items
            .Select((loop, index) => RequireEntityTag(
                Evaluate(loop),
                $"Plane Surface {tag} curve loop at index {index}"))
            .ToArray();
        return _geometry.AddPlaneSurface(tag, loopTags);
    }

    public Value VisitCurvesInSurfaceStatement(CurvesInSurfaceStmt node)
    {
        int[] curves = node.Curves.Items
            .Select((curve, index) => RequireEntityTag(
                Evaluate(curve),
                $"Embedded curve tag at index {index}"))
            .ToArray();
        if (node.Surfaces.Items.Count != 1)
        {
            throw new InvalidOperationException(
                $"Curve In Surface expects exactly one target surface, but received {node.Surfaces.Items.Count}. " +
                "Hint: use Curve {curveTags} In Surface {surfaceTag}.");
        }

        int surface = RequireEntityTag(Evaluate(node.Surfaces.Items[0]), "Embedded curve target surface");
        return _geometry.EmbedCurvesInSurface(curves, surface);
    }

    static int RequireNodeCount(Value value)
    {
        if (!value.IsNumber || !double.IsFinite(value.Number) ||
            value.Number < 2d || value.Number != Math.Truncate(value.Number) ||
            value.Number > int.MaxValue)
        {
            throw new InvalidOperationException(
                "Transfinite Curve node count must be a whole number of at least 2. " +
                "Hint: the count includes both end points.");
        }

        return (int)value.Number;
    }

    static int RequireOrientedCurveTag(Value value, string description)
    {
        if (!value.IsNumber || !double.IsFinite(value.Number) || value.Number == 0d ||
            value.Number != Math.Truncate(value.Number) ||
            value.Number < int.MinValue + 1d || value.Number > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"{description} must be a non-zero whole number. " +
                "Hint: use a positive curve tag or a negative tag to reverse its orientation.");
        }

        return (int)value.Number;
    }

    static double RequireTransfiniteCoefficient(
        Value value,
        TransfiniteCurveDistribution distribution)
    {
        if (!value.IsNumber || !double.IsFinite(value.Number) || value.Number <= 0d)
        {
            throw new InvalidOperationException(
                $"Transfinite Curve {distribution} coefficient must be a finite positive number.");
        }

        return value.Number;
    }

    Value EvaluateExplicitLoop(ForStmt node)
    {
        Value result = 0d;
        int iterations = 0;
        using (_scope.Open())
        {
            if (node.Iterator is not Token iterator)
            {
                throw new InvalidOperationException(
                    "An explicit value loop requires an iterator name. Hint: use 'For item In { ... }'.");
            }

            _scope.Declare(iterator.Text, 0d);
            foreach (Expr item in node.Items!)
            {
                if (++iterations > MaximumLoopIterations)
                {
                    throw new InvalidOperationException(
                        $"Loop exceeded the limit of {MaximumLoopIterations} iterations.");
                }

                _scope.TryAssign(iterator.Text, Evaluate(item));
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
