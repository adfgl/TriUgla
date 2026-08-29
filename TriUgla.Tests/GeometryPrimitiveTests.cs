using TriUgla.Script;

namespace TriUgla.Tests;

public class GeometryPrimitiveTests
{
    [Fact]
    public void Evaluate_PointDeclaration_AddsPointToGeometryModel()
    {
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(SyntaxTree.Parse("Point(1) = {1, 2, 3, 0.25};").Root);

        ScriptPoint point = result.As<ScriptPoint>();
        Assert.Same(point, evaluator.Geometry.Points[1]);
        Assert.Equal(1, point.X);
        Assert.Equal(2, point.Y);
        Assert.Equal(3, point.Z);
        Assert.Equal(.25, point.MeshSize);
        Assert.Equal("Point(1) = {1, 2, 3, 0.25};", point.ToString());
    }

    [Fact]
    public void Evaluate_PointWithoutMeshSize_LeavesMeshSizeUnset()
    {
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse("Point(2) = {0, 0, 0};").Root);

        Assert.Null(evaluator.Geometry.Points[2].MeshSize);
    }

    [Fact]
    public void Evaluate_LineDeclaration_ResolvesItsEndpointPoints()
    {
        const string source =
            "Point(1) = {0, 0, 0};\n" +
            "Point(2) = {1, 0, 0};\n" +
            "Line(1) = {1, 2};";
        var evaluator = new EvaluationVisitor();

        ScriptLine line = evaluator.Evaluate(SyntaxTree.Parse(source).Root).As<ScriptLine>();

        Assert.Same(evaluator.Geometry.Points[1], line.Start);
        Assert.Same(evaluator.Geometry.Points[2], line.End);
        Assert.Same(line, evaluator.Geometry.Lines[1]);
        Assert.Equal("Line(1) = {1, 2};", line.ToString());
    }

    [Fact]
    public void Evaluate_Primitives_CanUseExpressionsAndLoopTags()
    {
        const string source =
            "size = 0.1;\n" +
            "For i In {1:3}\n" +
            "  Point(i) = {i, i * 2, 0, size};\n" +
            "EndFor\n" +
            "Line(1) = {1, 2};";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(3, evaluator.Geometry.Points.Count);
        Assert.Equal(2, evaluator.Geometry.Points[1].Y);
        Assert.Equal(.1, evaluator.Geometry.Points[3].MeshSize);
        Assert.Single(evaluator.Geometry.Lines);
    }

    [Fact]
    public void Evaluate_PointCall_ReturnsRegisteredPoint()
    {
        const string source = "Point(1) = {1, 2, 3, 0.25}; Print(Point(1));";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Value printed = Assert.Single(evaluator.PrintedValues);
        Assert.Same(evaluator.Geometry.Points[1], printed.As<ScriptPoint>());
        Assert.Equal("Point(1) = {1, 2, 3, 0.25};", printed.ToString());
    }

    [Fact]
    public void Evaluate_PointCall_WithUnknownTag_SuggestsDeclaration()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("Print(Point(7));").Root));

        Assert.Contains("Point(7) is not registered", exception.Message);
        Assert.Contains("declare Point(7)", exception.Message);
    }

    [Fact]
    public void Evaluate_LineCall_ReturnsRegisteredLineWithFullDeclaration()
    {
        const string source =
            "Point(1) = {0, 0, 0, 0.1};\n" +
            "Point(2) = {1, 0, 0, 0.1};\n" +
            "Line(1) = {1, 2};\n" +
            "Print(Line(1));";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Value printed = Assert.Single(evaluator.PrintedValues);
        Assert.Same(evaluator.Geometry.Lines[1], printed.As<ScriptLine>());
        Assert.Equal("Line(1) = {1, 2};", printed.ToString());
    }

    [Fact]
    public void Evaluate_LineCall_WithUnknownTag_SuggestsDeclaration()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("Print(Line(4));").Root));

        Assert.Contains("Line(4) is not registered", exception.Message);
        Assert.Contains("declare Line(4)", exception.Message);
    }

    [Theory]
    [InlineData("Point()")]
    [InlineData("Point(1, 2)")]
    public void Evaluate_PointCall_WithWrongArgumentCount_ExplainsSyntax(string call)
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse($"Print({call});").Root));

        Assert.Contains("expects exactly one tag", exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Fact]
    public void Evaluate_LineWithMissingPoint_ExplainsDeclarationOrder()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("Point(1) = {0, 0, 0}; Line(1) = {1, 2};").Root));

        Assert.Contains("references point 2", exception.Message);
        Assert.Contains("point is not declared", exception.Message);
        Assert.Contains("declare Point(2) before this line", exception.Message);
    }

    [Fact]
    public void Evaluate_DuplicatePrimitiveTag_SuggestsUniqueTag()
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse(
                "Point(1) = {0, 0, 0}; Point(1) = {1, 1, 1};").Root));

        Assert.Contains("already declared", exception.Message);
        Assert.Contains("unique point tag", exception.Message);
    }

    [Theory]
    [InlineData("Point(1) = {0, 0};", "expects 3 coordinates")]
    [InlineData("Point(1.5) = {0, 0, 0};", "positive whole number")]
    [InlineData("Line(1) = {1};", "expects exactly 2 point tags")]
    [InlineData("Circle(1) = {1, 2};", "Assignment to function call 'Circle(...)'")]
    public void Evaluate_InvalidPrimitiveDeclaration_ProvidesHelpfulError(
        string source,
        string expected)
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse(source).Root));

        Assert.Contains(expected, exception.Message);
        Assert.Contains("Hint:", exception.Message);
    }

    [Fact]
    public void Evaluate_TransfiniteCurve_StoresGmshProgressionOptionsForEveryCurve()
    {
        const string source =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Point(3) = {2, 0, 0};\n" +
            "Line(1) = {1, 2}; Line(2) = {2, 3};\n" +
            "nodes = 11; Transfinite Curve{-1, 2} = nodes Using Progression 1.2;";
        var evaluator = new EvaluationVisitor();

        var result = evaluator.Evaluate(SyntaxTree.Parse(source).Root).As<TransfiniteCurveConstraint>();

        Assert.Equal([-1, 2], result.OrientedCurveTags);
        Assert.Equal(11, result.NodeCount);
        Assert.Equal(TransfiniteCurveDistribution.Progression, result.Distribution);
        Assert.Equal(1.2, result.Coefficient);
        Assert.Same(result, evaluator.Geometry.TransfiniteCurves[1]);
        Assert.Same(result, evaluator.Geometry.TransfiniteCurves[2]);
    }

    [Fact]
    public void Evaluate_TransfiniteCurve_SupportsBumpAndUniformDefaults()
    {
        const string prefix =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Line(1) = {1, 2};";

        var bumpEvaluator = new EvaluationVisitor();
        var bump = bumpEvaluator.Evaluate(
            SyntaxTree.Parse(prefix + " Transfinite Curve{1} = 9 Using Bump 0.5;").Root)
            .As<TransfiniteCurveConstraint>();
        var uniformEvaluator = new EvaluationVisitor();
        var uniform = uniformEvaluator.Evaluate(
            SyntaxTree.Parse(prefix + " Transfinite Curve{1} = 9;").Root)
            .As<TransfiniteCurveConstraint>();

        Assert.Equal(TransfiniteCurveDistribution.Bump, bump.Distribution);
        Assert.Equal(.5, bump.Coefficient);
        Assert.Equal(TransfiniteCurveDistribution.Progression, uniform.Distribution);
        Assert.Equal(1, uniform.Coefficient);
    }

    [Fact]
    public void Evaluate_TransfiniteCurveAll_AppliesConstraintToEveryDeclaredCurve()
    {
        const string source =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Point(3) = {2, 0, 0};\n" +
            "Line(1) = {1, 2}; Line(2) = {2, 3}; Transfinite Curve{All} = 7;";
        var evaluator = new EvaluationVisitor();

        evaluator.Evaluate(SyntaxTree.Parse(source).Root);

        Assert.Equal(2, evaluator.Geometry.TransfiniteCurves.Count);
        Assert.All(evaluator.Geometry.TransfiniteCurves.Values, item => Assert.Equal(7, item.NodeCount));
    }

    [Fact]
    public void Evaluate_LineLoop_StoresOrientedCurveTags()
    {
        const string source =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Point(3) = {1, 1, 0};\n" +
            "Line(1) = {1, 2}; Line(2) = {2, 3}; Line loop(1) = {1, -2};";
        var evaluator = new EvaluationVisitor();

        var loop = evaluator.Evaluate(SyntaxTree.Parse(source).Root).As<ScriptCurveLoop>();

        Assert.Equal([1, -2], loop.OrientedCurveTags);
        Assert.Same(loop, evaluator.Geometry.CurveLoops[1]);
    }

    [Theory]
    [InlineData("Transfinite Curve{1} = 1;", "at least 2")]
    [InlineData("Transfinite Curve{2} = 11;", "Line(2) is not declared")]
    [InlineData("Transfinite Curve{1} = 11 Using Bump 0;", "finite positive")]
    public void Evaluate_InvalidTransfiniteCurve_ProvidesHelpfulError(string statement, string expected)
    {
        const string prefix =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Line(1) = {1, 2};";
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse(prefix + statement).Root));

        Assert.Contains(expected, exception.Message);
    }

    [Fact]
    public void Evaluate_PlaneSurface_ResolvesExteriorAndHoleCurveLoops()
    {
        const string source =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Point(3) = {1, 1, 0}; Point(4) = {0, 1, 0};\n" +
            "Line(1) = {1, 2}; Line(2) = {2, 3}; Line(3) = {3, 4}; Line(4) = {4, 1};\n" +
            "Curve Loop(1) = {1, 2, 3, 4}; Plane Surface(1) = {1};";
        var evaluator = new EvaluationVisitor();

        var surface = evaluator.Evaluate(SyntaxTree.Parse(source).Root).As<ScriptPlaneSurface>();

        Assert.Equal(1, surface.Tag);
        Assert.Same(evaluator.Geometry.CurveLoops[1], Assert.Single(surface.CurveLoops));
        Assert.Same(surface, evaluator.Geometry.PlaneSurfaces[1]);
        Assert.Equal("Plane Surface(1) = {1};", surface.ToString());
    }

    [Theory]
    [InlineData("Plane Surface(1) = {};", "at least one curve loop")]
    [InlineData("Plane Surface(1) = {2};", "Curve Loop(2)")]
    public void Evaluate_InvalidPlaneSurface_ProvidesHelpfulError(string statement, string expected)
    {
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse(statement).Root));

        Assert.Contains(expected, exception.Message);
    }

    [Fact]
    public void Evaluate_ComplexSurfaceWithEmbeddedLines_RunsAsSingleGmshStyleScript()
    {
        const string source = """
            Point(1) = {0.0, 0.0, 0.0, 0.100};
            Point(2) = {2.0, 0.0, 0.0, 0.100};
            Point(3) = {2.0, 2.0, 0.0, 0.100};
            Point(4) = {0.0, 2.0, 0.0, 0.100};
            Point(5) = {0.4, 0.5, 0.0, 0.100};
            Point(6) = {1.4, 0.5, 0.0, 0.100};
            Point(7) = {1.4, 1.5, 0.0, 0.100};
            Point(8) = {0.4, 1.5, 0.0, 0.100};
            Line(1) = {1, 2};
            Transfinite Curve{1} = 8 Using Progression 1;
            Line(2) = {2, 3};
            Transfinite Curve{2} = 8 Using Progression 1;
            Line(3) = {3, 4};
            Transfinite Curve{3} = 8 Using Progression 1;
            Line(4) = {4, 1};
            Transfinite Curve{4} = 8 Using Progression 1;
            Line loop(1) = {1, 2, 3, 4};
            Plane Surface(1) = {1};
            Line(18) = {5, 6};
            Line(19) = {5, 6};
            Line(20) = {6, 7};
            Line(21) = {7, 8};
            Line(22) = {8, 5};
            Transfinite Curve{18, 19, 20, 21, 22} = 4 Using Progression 1;
            Line {18, 19, 20, 21, 22} In Surface {1};
            """;
        SyntaxTree tree = SyntaxTree.Parse(source);
        var evaluator = new EvaluationVisitor();

        Value result = evaluator.Evaluate(tree.Root);

        Assert.Empty(tree.Diagnostics);
        ScriptPlaneSurface surface = result.As<ScriptPlaneSurface>();
        Assert.Equal([18, 19, 20, 21, 22], surface.EmbeddedCurveTags.Order());
        Assert.Equal(5, evaluator.Geometry.TransfiniteCurves.Keys.Count(tag => tag >= 18));
    }

    [Theory]
    [InlineData("Line {2} In Surface {1};", "Line(2)")]
    [InlineData("Line {1} In Surface {2};", "Plane Surface(2)")]
    public void Evaluate_InvalidEmbeddedCurveConstraint_ProvidesHelpfulError(
        string constraint,
        string expected)
    {
        const string prefix =
            "Point(1) = {0, 0, 0}; Point(2) = {1, 0, 0}; Line(1) = {1, 2}; " +
            "Curve Loop(1) = {1}; Plane Surface(1) = {1};";
        var evaluator = new EvaluationVisitor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse(prefix + constraint).Root));

        Assert.Contains(expected, exception.Message);
    }
}
