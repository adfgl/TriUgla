using TriUgla.Script;

namespace TriUgla.Tests;

public class ListIndexTests
{
    [Fact]
    public void Evaluate_SingleIndex_ReturnsZeroBasedElement()
    {
        Value result = Evaluate("values = {10, 20, 30}; values[1];");

        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void Evaluate_ChainedIndices_AccessesMatrixCell()
    {
        Value result = Evaluate("matrix = {{1, 2}, {3, 4}}; matrix[1][0];");

        Assert.Equal(3, result.Number);
    }

    [Fact]
    public void Evaluate_MultipleIndices_ReturnsSelectedValuesInOrder()
    {
        ScriptList result = Evaluate("values = {10, 20, 30}; values[{2, 0, 2}];").As<ScriptList>();

        Assert.Equal([30d, 10d, 30d], result.Items.Select(value => value.Number));
    }

    [Theory]
    [InlineData("values = {10}; values[1];", "valid indices are 0 through 0")]
    [InlineData("values = {10}; values[-1];", "out of range")]
    [InlineData("values = {10}; values[0.5];", "whole number")]
    [InlineData("value = 10; value[0];", "square brackets only after a list")]
    public void Evaluate_InvalidIndex_ProvidesHelpfulError(string source, string expected)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Evaluate(source));

        Assert.Contains(expected, exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    static Value Evaluate(string source)
        => new EvaluationVisitor().Evaluate(SyntaxTree.Parse(source).Root);
}
