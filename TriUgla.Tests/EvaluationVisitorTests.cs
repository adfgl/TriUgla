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
}
