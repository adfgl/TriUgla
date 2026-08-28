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
    public void Accept_DispatchesToEvaluationVisitor()
    {
        SyntaxTree tree = SyntaxTree.Parse("10;");

        Value result = tree.Root.Accept(new EvaluationVisitor());

        Assert.Equal(10, result.Number);
    }
}
