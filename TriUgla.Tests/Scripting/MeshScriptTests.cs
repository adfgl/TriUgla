using TriUgla.Script;

namespace TriUgla.Tests;

public class MeshScriptTests
{
    const string Script = """
        Mesh.ElementOrder = 1;
        Mesh.SecondOrderIncomplete = 1;
        Mesh.Algorithm = 8;
        Mesh.CharacteristicLengthFromPoints = 1;
        Mesh.CharacteristicLengthExtendFromBoundary = 1;
        Mesh.CharacteristicLengthMin =  0.100;
        Mesh.CharacteristicLengthMax =  0.100;
        Mesh.SubdivisionAlgorithm  = 0;
        Mesh.RecombinationAlgorithm = 0;
        Mesh.RecombineAll = 1;
        Mesh 2;
        Coherence Mesh;
        RenumberMeshNodes;
        RenumberMeshElements;
        """;

    [Fact]
    public void Parse_GmshMeshConfiguration_CreatesDedicatedStatements()
    {
        SyntaxTree tree = SyntaxTree.Parse(Script);

        Assert.Empty(tree.Diagnostics);
        Assert.Equal(10, tree.Root.Statements.OfType<AssignmentStmt>().Count());
        Assert.All(
            tree.Root.Statements.OfType<AssignmentStmt>(),
            statement => Assert.IsType<MemberAccessExpr>(statement.Target));
        Assert.Equal(4, tree.Root.Statements.OfType<MeshCommandStmt>().Count());
    }

    [Fact]
    public void Evaluate_GmshMeshConfiguration_RecordsOptionsAndOrderedCommands()
    {
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(Script).Root);

        Assert.Equal(1, evaluator.Mesh.Options["ElementOrder"]);
        Assert.Equal(1, evaluator.Mesh.Options["SecondOrderIncomplete"]);
        Assert.Equal(8, evaluator.Mesh.Options["Algorithm"]);
        Assert.Equal(0.1, evaluator.Mesh.Options["CharacteristicLengthMin"]);
        Assert.Equal(0.1, evaluator.Mesh.Options["CharacteristicLengthMax"]);
        Assert.Equal(1, evaluator.Mesh.Options["RecombineAll"]);
        Assert.Equal(
            [
                new MeshScriptCommand(MeshScriptCommandKind.Generate, 2),
                new MeshScriptCommand(MeshScriptCommandKind.Coherence),
                new MeshScriptCommand(MeshScriptCommandKind.RenumberNodes),
                new MeshScriptCommand(MeshScriptCommandKind.RenumberElements)
            ],
            evaluator.Mesh.Commands);
    }

    [Fact]
    public void Evaluate_MeshOptionValue_CanUseAnExpression()
    {
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse("order = 1; Mesh.ElementOrder = order + 1;").Root);

        Assert.Equal(2, evaluator.Mesh.Options["ElementOrder"]);
    }

    [Fact]
    public void Evaluate_MeshOption_CanBeReadThroughGeneralPropertyAccess()
    {
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(
            SyntaxTree.Parse("Mesh.ElementOrder = 2; Mesh.ElementOrder;").Root);

        Assert.Equal(2, result.Number);
    }

    [Theory]
    [InlineData("Mesh 0;")]
    [InlineData("Mesh 2.5;")]
    [InlineData("Mesh 4;")]
    public void Evaluate_InvalidMeshDimension_Throws(string source)
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => evaluator.Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains("integer from 1 through 3", exception.Message);
        Assert.Empty(evaluator.Mesh.Commands);
    }

    [Fact]
    public void Evaluate_UnknownMeshOption_Throws()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => evaluator.Evaluate(SyntaxTree.Parse("Mesh.NotAnOption = 1;").Root));

        Assert.Contains("not supported", exception.Message);
    }
}
