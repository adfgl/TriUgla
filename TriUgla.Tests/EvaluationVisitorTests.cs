using TriUgla.Script;

namespace TriUgla.Tests;

public class EvaluationVisitorTests
{
    [Fact]
    public void Evaluate_CompilationUnit_EvaluatesAssignmentsAndExpressions()
    {
        SyntaxTree tree = SyntaxTree.Parse("value = 1 + 2 * 3; value + 1;");
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(tree.Root);

        Assert.Equal(8, result.Number);
        Assert.True(evaluator.Scope.TryGetValue("value", out Value variable));
        Assert.Equal(7, variable.Number);
    }

    [Theory]
    [InlineData("-2 + 5;", 3)]
    [InlineData("2 < 3;", 1)]
    [InlineData("2 >= 3;", 0)]
    [InlineData("!0;", 1)]
    [InlineData("\"Tri\" + \"Ugla\" == \"TriUgla\";", 1)]
    public void Evaluate_Operators_ReturnExpectedValue(string source, double expected)
    {
        Value result = new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(expected, result.Number);
    }

    [Fact]
    public void Evaluate_Block_RemovesVariablesDeclaredInBlock()
    {
        SyntaxTree tree = SyntaxTree.Parse("{ inner = 42; }");
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(tree.Root);

        Assert.Equal(42, result.Number);
        Assert.False(evaluator.Scope.IsDeclared("inner"));
    }

    [Fact]
    public void Evaluate_List_ReturnsScriptList()
    {
        SyntaxTree tree = SyntaxTree.Parse("values = {1, 2 + 3}; values;");

        ScriptList list = new EvaluationVisitor().Evaluate(tree.Root).As<ScriptList>();

        Assert.Equal([1d, 5d], list.Items.Select(item => item.Number));
    }

    [Fact]
    public void Evaluate_Call_InvokesRegisteredFunction()
    {
        var functions = new Dictionary<string, Func<IReadOnlyList<Value>, Value>>
        {
            ["Double"] = arguments => arguments[0].Number * 2
        };
        SyntaxTree tree = SyntaxTree.Parse("Double(6);");

        Value result = new EvaluationVisitor(functions: functions).Evaluate(tree.Root);

        Assert.Equal(12, result.Number);
    }

    [Fact]
    public void Evaluate_Print_EmitsEachValueInEvaluationOrder()
    {
        SyntaxTree tree = SyntaxTree.Parse("Print(1); Print(2 + 3); Print(\"done\");");
        var evaluator = new EvaluationVisitor();
        var printed = new List<string>();
        evaluator.Printed += value => printed.Add(value.ToString());

        evaluator.Evaluate(tree.Root);

        Assert.Equal(["1", "5", "done"], printed);
        Assert.Equal(printed, evaluator.PrintedValues.Select(value => value.ToString()));
    }

    [Theory]
    [InlineData("Print();", 0)]
    [InlineData("Print(1, 2);", 2)]
    public void Evaluate_Print_WithWrongArgumentCount_Throws(string source, int count)
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => evaluator.Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains($"received {count}", exception.Message);
    }

    [Fact]
    public void Accept_DispatchesToEvaluationVisitor()
    {
        SyntaxTree tree = SyntaxTree.Parse("10;");

        Value result = tree.Root.Accept(new EvaluationVisitor());

        Assert.Equal(10, result.Number);
    }

    [Theory]
    [InlineData(12, "large")]
    [InlineData(7, "medium")]
    [InlineData(2, "small")]
    public void Evaluate_GmshConditional_ExecutesFirstMatchingBranch(
        double input,
        string expected)
    {
        SyntaxTree tree = SyntaxTree.Parse(
            $"value = {input};\n" +
            "If (value > 10)\n" +
            "  Print(\"large\");\n" +
            "ElseIf (value > 5)\n" +
            "  Print(\"medium\");\n" +
            "Else\n" +
            "  Print(\"small\");\n" +
            "EndIf");
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(tree.Root);

        Assert.Equal(expected, Assert.Single(evaluator.PrintedValues).ToString());
    }

    [Fact]
    public void Evaluate_ConditionalWithNoMatchingBranch_DoesNothing()
    {
        SyntaxTree tree = SyntaxTree.Parse("If (0)\nPrint(\"no\");\nEndIf");
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(tree.Root);

        Assert.Equal(0, result.Number);
        Assert.Empty(evaluator.PrintedValues);
    }

    [Theory]
    [InlineData("For i In {1:3}\nPrint(i);\nEndFor", new[] { "1", "2", "3" })]
    [InlineData("For i In {5:1:-2}\nPrint(i);\nEndFor", new[] { "5", "3", "1" })]
    [InlineData("For (1:3)\nPrint(\"tick\");\nEndFor", new[] { "tick", "tick", "tick" })]
    public void Evaluate_GmshLoop_PrintsEveryIterationInOrder(
        string source,
        string[] expected)
    {
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(expected, evaluator.PrintedValues.Select(value => value.ToString()));
    }

    [Fact]
    public void Evaluate_LoopIterator_IsScopedToLoop()
    {
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse("For i In {1:2}\nPrint(i);\nEndFor").Root);

        Assert.False(evaluator.Scope.IsDeclared("i"));
    }

    [Fact]
    public void Evaluate_ForInExplicitList_VisitsEveryItemInOrder()
    {
        const string source = "size = 4;\nFor item In { 1, 2, 3 }\n Print(item);\nEndFor";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(
            ["1", "2", "3"],
            evaluator.PrintedValues.Select(value => value.ToString()));
    }

    [Fact]
    public void Evaluate_ForInExplicitList_EvaluatesItemExpressions()
    {
        const string source = "scale = 2;\nFor item In { scale, scale + 1, scale * 2 }\n Print(item);\nEndFor";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(
            ["2", "3", "4"],
            evaluator.PrintedValues.Select(value => value.ToString()));
    }

    [Fact]
    public void Evaluate_LoopWithZeroStep_Throws()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("For i In {1:2:0}\nPrint(i);\nEndFor").Root));

        Assert.Contains("non-zero", exception.Message);
    }

    [Fact]
    public void Evaluate_NestedLoops_ExecuteCompleteCartesianRange()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "For i In {1:2}\n" +
            "  For j In {1:2}\n" +
            "    Print(i * 10 + j);\n" +
            "  EndFor\n" +
            "EndFor");
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(tree.Root);

        Assert.Equal(
            ["11", "12", "21", "22"],
            evaluator.PrintedValues.Select(value => value.ToString()));
    }

    [Fact]
    public async Task EvaluateAsync_Cancellation_StopsRunningLoop()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "For i In {1:1000000}\n" +
            "  Print(i);\n" +
            "EndFor");
        using var cancellation = new CancellationTokenSource();
        var evaluator = new EvaluationVisitor();
        evaluator.Printed += _ =>
        {
            if (evaluator.PrintedValues.Count == 3)
            {
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await evaluator.EvaluateAsync(tree.Root, cancellation.Token));

        Assert.Equal(["1", "2", "3"], evaluator.PrintedValues.Select(value => value.ToString()));
    }

    [Theory]
    [InlineData("({1, 2, 3} + {4, 5, 6});", new[] { 5d, 7d, 9d })]
    [InlineData("({1, 2, 3} + 10);", new[] { 11d, 12d, 13d })]
    [InlineData("10 + {1, 2, 3};", new[] { 11d, 12d, 13d })]
    [InlineData("({1, 2, 3} * {4, 5, 6});", new[] { 4d, 10d, 18d })]
    [InlineData("({1, 2, 3} * 2);", new[] { 2d, 4d, 6d })]
    [InlineData("2 * {1, 2, 3};", new[] { 2d, 4d, 6d })]
    [InlineData("({8, 9, 10} / {2, 3, 5});", new[] { 4d, 3d, 2d })]
    [InlineData("({8, 10} / 2);", new[] { 4d, 5d })]
    [InlineData("12 / {2, 3, 4};", new[] { 6d, 4d, 3d })]
    public void Evaluate_ListArithmetic_ReturnsElementWiseResults(
        string source,
        double[] expected)
    {
        ScriptList result = new EvaluationVisitor()
            .Evaluate(SyntaxTree.Parse(source).Root)
            .As<ScriptList>();

        Assert.Equal(expected, result.Items.Select(value => value.Number));
    }

    [Fact]
    public void Evaluate_ListArithmetic_WithDifferentLengths_ExplainsBothLengths()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse("({1, 2} + {3});").Root));

        Assert.Contains("different lengths (2 and 1)", exception.Message);
    }

    [Fact]
    public void Evaluate_ListArithmetic_WithNonNumericItem_IdentifiesIndex()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse("({1, \"text\"} * 2);").Root));

        Assert.Contains("left list at index 1", exception.Message);
        Assert.Contains("a string", exception.Message);
    }

    [Fact]
    public void Evaluate_ListDivision_ByZero_IdentifiesIndex()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse("({4, 8} / {2, 0});").Root));

        Assert.Contains("zero at list index 1", exception.Message);
    }

    [Fact]
    public void Evaluate_UnsupportedListOperator_ListsSupportedOperators()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse("({1, 2} - 1);").Root));

        Assert.Contains("cannot be applied when an operand is a list", exception.Message);
        Assert.Contains("use '+', '*' or '/'", exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Theory]
    [InlineData("\"text\" * 2;")]
    [InlineData("2 + \"text\";")]
    [InlineData("\"left\" / \"right\";")]
    public void Evaluate_UnsupportedOperands_DescribesTypesAndSuggestsFix(string source)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains("Operator", exception.Message);
        Assert.Contains("cannot be applied", exception.Message);
        Assert.Contains("Hint:", exception.Message);
        Assert.Contains("number", exception.Message);
        Assert.Contains("string", exception.Message);
    }

    [Fact]
    public void Evaluate_ScalarDivisionByZero_SuggestsNonZeroOperand()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse("10 / 0;").Root));

        Assert.Contains("Operator '/'", exception.Message);
        Assert.Contains("Hint:", exception.Message);
        Assert.Contains("non-zero", exception.Message);
    }

    [Theory]
    [InlineData("Acos(1);", 0)]
    [InlineData("Asin(0);", 0)]
    [InlineData("Atan(0);", 0)]
    [InlineData("Atan2(0, 1);", 0)]
    [InlineData("Ceil(1.2);", 2)]
    [InlineData("Cos(0);", 1)]
    [InlineData("Cosh(0);", 1)]
    [InlineData("Exp(0);", 1)]
    [InlineData("Fabs(-2);", 2)]
    [InlineData("Fmod(5, 2);", 1)]
    [InlineData("Floor(1.8);", 1)]
    [InlineData("Hypot(3, 4);", 5)]
    [InlineData("Log(1);", 0)]
    [InlineData("Log10(1);", 0)]
    [InlineData("Max(3, 7);", 7)]
    [InlineData("Min(3, 7);", 3)]
    [InlineData("Modulo(5, 2);", 1)]
    [InlineData("Round(1.5);", 2)]
    [InlineData("Sqrt(4);", 2)]
    [InlineData("Sin(0);", 0)]
    [InlineData("Sinh(0);", 0)]
    [InlineData("Step(-1);", 0)]
    [InlineData("Step(0);", 1)]
    [InlineData("Tan(0);", 0)]
    [InlineData("Tanh(0);", 0)]
    public void Evaluate_StandardMathFunction_ReturnsExpectedResult(
        string source,
        double expected)
    {
        Value result = new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(expected, result.Number, 12);
    }

    [Fact]
    public void Evaluate_Rand_ReturnsValueWithinRequestedRange()
    {
        Value result = new EvaluationVisitor().Evaluate(SyntaxTree.Parse("Rand(5);").Root);

        Assert.InRange(result.Number, 0d, 5d);
    }

    [Theory]
    [InlineData("Sqrt(-1);", "greater than or equal to zero")]
    [InlineData("Log(0);", "greater than zero")]
    [InlineData("Acos(2);", "between -1 and 1")]
    [InlineData("Sin();", "expects exactly 1 argument")]
    [InlineData("Max();", "expects at least 1 argument")]
    [InlineData("Cos(\"zero\");", "argument 1 to be a number")]
    [InlineData("Fmod(1, 0);", "argument 2 a non-zero number")]
    public void Evaluate_InvalidMathFunctionCall_ProvidesHelpfulError(
        string source,
        string expectedMessage)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Fact]
    public void StandardMathFunctions_ContainsCompleteGmshMathSet()
    {
        string[] expected =
        [
            "Acos", "Asin", "Atan", "Atan2", "Ceil", "Cos", "Cosh", "Exp", "Fabs",
            "Fmod", "Floor", "Hypot", "Log", "Log10", "Max", "Min", "Modulo", "Rand",
            "Round", "Sqrt", "Sin", "Sinh", "Step", "Tan", "Tanh"
        ];

        Assert.Equal(expected.Order(), StandardMathFunctions.All.Keys.Order());
        Assert.Contains("Print", StandardMathFunctions.Names);
    }

    [Theory]
    [InlineData("Min(8);", 8)]
    [InlineData("Min(8, -2, 4, 1, 9);", -2)]
    [InlineData("Max(8);", 8)]
    [InlineData("Max(8, -2, 4, 1, 9);", 9)]
    public void Evaluate_MinMax_AcceptsAnyPositiveNumberOfArguments(
        string source,
        double expected)
    {
        Value result = new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(expected, result.Number);
    }

    [Fact]
    public void Evaluate_VariadicMinMax_IdentifiesNonNumericArgumentPosition()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse("Max(1, 2, \"three\", 4);").Root));

        Assert.Contains("argument 3", exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Fact]
    public void Evaluate_MatrixTimesVector_ReturnsRowDotProducts()
    {
        const string source =
            "matrix = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};\n" +
            "vector = {11, 22, 33};\n" +
            "matrix * vector;";

        ScriptList result = new EvaluationVisitor()
            .Evaluate(SyntaxTree.Parse(source).Root)
            .As<ScriptList>();

        Assert.Equal([154d, 352d, 550d], result.Items.Select(value => value.Number));
    }

    [Fact]
    public void Evaluate_VectorTimesMatrix_ReturnsColumnDotProducts()
    {
        const string source =
            "vector = {1, 2};\n" +
            "matrix = {{3, 4, 5}, {6, 7, 8}};\n" +
            "vector * matrix;";

        ScriptList result = new EvaluationVisitor()
            .Evaluate(SyntaxTree.Parse(source).Root)
            .As<ScriptList>();

        Assert.Equal([15d, 18d, 21d], result.Items.Select(value => value.Number));
    }

    [Fact]
    public void Evaluate_MatrixTimesMatrix_PerformsMatrixMultiplication()
    {
        const string source =
            "left = {{1, 2, 3}, {4, 5, 6}};\n" +
            "right = {{7, 8}, {9, 10}, {11, 12}};\n" +
            "left * right;";

        ScriptList result = new EvaluationVisitor()
            .Evaluate(SyntaxTree.Parse(source).Root)
            .As<ScriptList>();

        Assert.Equal([58d, 64d], result.Items[0].As<ScriptList>().Items.Select(value => value.Number));
        Assert.Equal([139d, 154d], result.Items[1].As<ScriptList>().Items.Select(value => value.Number));
    }

    [Theory]
    [InlineData("matrix * 2;", new[] { 2d, 4d, 6d, 8d })]
    [InlineData("2 + matrix;", new[] { 3d, 4d, 5d, 6d })]
    [InlineData("matrix / 2;", new[] { .5d, 1d, 1.5d, 2d })]
    public void Evaluate_MatrixAndScalar_BroadcastsAcrossEveryCell(
        string operation,
        double[] expected)
    {
        SyntaxTree tree = SyntaxTree.Parse("matrix = {{1, 2}, {3, 4}};\n" + operation);

        ScriptList matrix = new EvaluationVisitor().Evaluate(tree.Root).As<ScriptList>();
        double[] actual = matrix.Items
            .SelectMany(row => row.As<ScriptList>().Items)
            .Select(value => value.Number)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_MatrixMultiplication_WithWrongDimensions_ExplainsInnerDimensions()
    {
        const string source = "matrix = {{1, 2}, {3, 4}}; vector = {1, 2, 3}; matrix * vector;";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains("matrix columns (2)", exception.Message);
        Assert.Contains("vector length (3)", exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Fact]
    public void Evaluate_RaggedMatrix_IdentifiesBadRow()
    {
        const string source = "matrix = {{1, 2}, {3}}; matrix * 2;";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains("ragged", exception.Message);
        Assert.Contains("row 1", exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Fact]
    public void Evaluate_MatrixTimesVector_PrintsExpectedValue()
    {
        const string source =
            "matrix = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};\n" +
            "vector = {11, 22, 33};\n" +
            "Print(matrix * vector);";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal("{154, 352, 550}", Assert.Single(evaluator.PrintedValues).ToString());
    }
}
