using TriUgla.Script;

namespace TriUgla.Tests;

public class MeshScriptTests
{
    const string Script = """
        Point(1) = {0, 0, 0};
        Point(2) = {1, 0, 0};
        Point(3) = {0, 1, 0};
        Line(1) = {1, 2};
        Line(2) = {2, 3};
        Line(3) = {3, 1};
        Curve Loop(1) = {1, 2, 3};
        Plane Surface(1) = {1};
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
    public void MeshMetricFormattingPreservesSmallNonZeroValues()
    {
        var metric = new ScriptMeshMetric(2, 0.000001, 0.000002, 0.000003);

        string text = metric.Format();

        Assert.Contains("1E-06", text);
        Assert.Contains("2E-06", text);
        Assert.Contains("3E-06", text);
        Assert.DoesNotContain("0.00", text);
    }

    [Fact]
    public void Parse_GmshMeshConfiguration_CreatesDedicatedStatements()
    {
        SyntaxTree tree = SyntaxTree.Parse(Script);

        Assert.Empty(tree.Diagnostics);
        AssignmentStmt[] meshOptions = tree.Root.Statements.OfType<AssignmentStmt>()
            .Where(statement => statement.Target is MemberAccessExpr)
            .ToArray();
        Assert.Equal(10, meshOptions.Length);
        Assert.All(
            meshOptions,
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
        Assert.NotNull(evaluator.Mesh.GeneratedMesh);
        Assert.Contains(evaluator.Mesh.GeneratedMesh.Faces, face => face.Kind == FaceKind.Island);
        Assert.Contains(evaluator.Mesh.GeneratedMesh.Faces, face => face.ContainsSuperStructure);
        Assert.Contains(evaluator.Mesh.GeneratedMesh.Faces, face => !face.ContainsSuperStructure);
        Assert.True(evaluator.Mesh.GeneratedMesh.Metrics.Angle.Count > 0);
        Assert.True(evaluator.Mesh.GeneratedMesh.Metrics.EdgeLength.Count > 0);
        Assert.True(evaluator.Mesh.GeneratedMesh.Metrics.FaceArea.Count > 0);
        Assert.True(evaluator.Mesh.GeneratedMesh.Metrics.FaceArea.Min > 0d);
        Assert.True(evaluator.Mesh.GeneratedMesh.Metrics.FaceArea.Average > 0d);
        Assert.True(evaluator.Mesh.GeneratedMesh.Metrics.FaceArea.Max > 0d);
        Assert.Equal(0, evaluator.Mesh.GeneratedMesh.Metrics.DegenerateFaces);
        Assert.Contains("Mesh metrics", evaluator.Mesh.GeneratedMesh.Metrics.ToString());
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

    [Fact]
    public void Evaluate_MeshCommandExplainsMissingSurface()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("Mesh 2;").Root));

        Assert.Contains("Mesh 2 failed", exception.Message);
        Assert.Contains("Plane Surface", exception.Message);
    }

    [Fact]
    public void Evaluate_MeshUsesLoopsHolesEmbeddedCurvesAndTransfiniteNodes()
    {
        const string source = """
            Point(1) = {0, 0, 0}; Point(2) = {4, 0, 0};
            Point(3) = {4, 4, 0}; Point(4) = {0, 4, 0};
            Point(5) = {1, 1, 0}; Point(6) = {2, 1, 0};
            Point(7) = {2, 2, 0}; Point(8) = {1, 2, 0};
            Point(9) = {0.5, 3, 0}; Point(10) = {3.5, 3, 0};
            Line(1) = {1, 2}; Line(2) = {2, 3}; Line(3) = {3, 4}; Line(4) = {4, 1};
            Line(5) = {5, 6}; Line(6) = {6, 7}; Line(7) = {7, 8}; Line(8) = {8, 5};
            Line(9) = {9, 10};
            Transfinite Curve {1} = 5;
            Curve Loop(1) = {1, 2, 3, 4};
            Curve Loop(2) = {5, 6, 7, 8};
            Plane Surface(1) = {1, 2};
            Curve {9} In Surface {1};
            Mesh 2;
            """;
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        ScriptMeshResult mesh = Assert.IsType<ScriptMeshResult>(evaluator.Mesh.GeneratedMesh);
        Assert.Contains(mesh.Faces, face => face.Kind == FaceKind.Island);
        Assert.Contains(mesh.Faces, face => face.Kind == FaceKind.Lake);
        Assert.Contains(mesh.Faces, face => face.Kind == FaceKind.Outside);
        Assert.True(mesh.Faces.Count > 10);
    }

    [Fact]
    public void Evaluate_MeshRoundsSampledCurveConnectionsToSixDecimalPlaces()
    {
        const string source = """
            Point(1) = {1, 0, 0};
            Point(2) = {0, 1, 0};
            Point(3) = {-1, 0, 0};
            Point(4) = {0, -1, 0};
            Point(5) = {0, 0, 0};
            Circle(1) = {1, 5, 2};
            Circle(2) = {2, 5, 3};
            Circle(3) = {3, 5, 4};
            Circle(4) = {4, 5, 1};
            Curve Loop(1) = {1, 2, 3, 4};
            Plane Surface(1) = {1};
            Mesh 2;
            """;
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.NotNull(evaluator.Mesh.GeneratedMesh);
        Assert.Contains(evaluator.Mesh.GeneratedMesh.Faces, face => face.Kind == FaceKind.Island);
    }

    [Fact]
    public void Evaluate_MeshProjectsSlopedPlanarSurfaceAndRestoresThreeDimensionalVertices()
    {
        const string source = """
            Point(1) = {0, 0, 0}; Point(2) = {1, 0, 1};
            Point(3) = {1, 1, 2}; Point(4) = {0, 1, 1};
            Line(1) = {1, 2}; Line(2) = {2, 3};
            Line(3) = {3, 4}; Line(4) = {4, 1};
            Curve Loop(1) = {1, 2, 3, 4};
            Plane Surface(1) = {1};
            Mesh 2;
            """;
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        ScriptMeshFace[] land = evaluator.Mesh.GeneratedMesh!.Faces
            .Where(face => face.Kind == FaceKind.Island)
            .ToArray();
        Assert.NotEmpty(land);
        Assert.All(
            land.SelectMany(face => face.Vertices),
            vertex => Assert.Equal(vertex.X + vertex.Y, vertex.Z, 6));
    }

    [Fact]
    public void Evaluate_MeshAllowsNonCoplanarElevationsOverValidXyFootprint()
    {
        const string source = """
            Point(1) = {0, 0, 0}; Point(2) = {1, 0, 1};
            Point(3) = {1, 1, 5}; Point(4) = {0, 1, -2};
            Line(1) = {1, 2}; Line(2) = {2, 3};
            Line(3) = {3, 4}; Line(4) = {4, 1};
            Curve Loop(1) = {1, 2, 3, 4};
            Plane Surface(1) = {1};
            Mesh 2;
            """;
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        ScriptMeshFace[] land = evaluator.Mesh.GeneratedMesh!.Faces
            .Where(face => face.Kind == FaceKind.Island)
            .ToArray();
        Assert.NotEmpty(land);
        double[] elevations = land.SelectMany(face => face.Vertices).Select(vertex => vertex.Z).ToArray();
        Assert.Contains(5d, elevations);
        Assert.Contains(-2d, elevations);
    }
}
