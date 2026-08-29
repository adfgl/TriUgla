using TriUgla.Script;

namespace TriUgla.Tests;

public class PhysicalPointTests
{
    const string Source = "Point(1) = {0, 0, 0, 0.1}; Physical Point(\"MyNamedPoint\") = {1};";

    [Fact]
    public void Parse_PhysicalPoint_CreatesDedicatedStatement()
    {
        SyntaxTree tree = SyntaxTree.Parse(Source);

        Assert.IsType<PhysicalPointStmt>(tree.Root.Statements[1]);
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Evaluate_PhysicalPoint_RetainsNameAndMembers()
    {
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(SyntaxTree.Parse(Source).Root);

        ScriptPhysicalPointGroup group = result.As<ScriptPhysicalPointGroup>();
        Assert.Equal("MyNamedPoint", group.Name);
        Assert.Same(evaluator.Geometry.Points[1], Assert.Single(group.Points));
        Assert.Equal(["MyNamedPoint"], evaluator.Geometry.Points[1].PhysicalNames);
        Assert.Same(group, evaluator.Geometry.PhysicalPoints["MyNamedPoint"]);
    }

    [Fact]
    public void Evaluate_PhysicalPointWithMissingMember_Throws()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("Physical Point(\"Missing\") = {7};").Root));

        Assert.Contains("Point(7)", exception.Message);
    }
}
