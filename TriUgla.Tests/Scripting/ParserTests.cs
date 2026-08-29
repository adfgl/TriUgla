using TriUgla.Script;

namespace TriUgla.Tests;

public class ParserTests
{
    [Fact]
    public void Parse_ValidGmshStyleStatements_ReturnsAstWithoutErrors()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "Point(1) = {0, 0, 0, 1};\nLine(1) = {1, 2};");

        Assert.False(tree.HasErrors);
        Assert.Empty(tree.Diagnostics);
        Assert.Equal(2, tree.Root.Statements.Count);
        Assert.All(tree.Root.Statements, statement => Assert.IsType<AssignmentStmt>(statement));
    }

    [Fact]
    public void Parse_BinaryExpression_UsesOperatorPrecedence()
    {
        SyntaxTree tree = SyntaxTree.Parse("value = 1 + 2 * 3;");
        var statement = Assert.IsType<AssignmentStmt>(Assert.Single(tree.Root.Statements));
        var addition = Assert.IsType<BinaryExpr>(statement.Value);

        Assert.Equal(TokenKind.Plus, addition.Operator.Kind);
        Assert.IsType<LiteralExpr>(addition.Left);
        Assert.Equal(TokenKind.Star, Assert.IsType<BinaryExpr>(addition.Right).Operator.Kind);
    }

    [Fact]
    public void Parse_MissingSemicolon_ReportsErrorAndContinuesNextLine()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "Point(1) = {0, 0}\nPoint(2) = {1, 1};");

        Diagnostic error = Assert.Single(tree.Diagnostics);
        Assert.Equal("TS1006", error.Code);
        Assert.Equal(2, tree.Root.Statements.Count);
        Assert.Equal(2, error.Span.Line);
    }

    [Fact]
    public void Parse_MalformedStatement_SuppressesCascadesAndRecoversAtSemicolon()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "Point( = {0}; Point(2) = {1, 1};");

        Assert.Single(tree.Diagnostics);
        Assert.Equal(2, tree.Root.Statements.Count);
        Assert.IsType<AssignmentStmt>(tree.Root.Statements[1]);
    }

    [Fact]
    public void Parse_UnclosedBlock_ReportsSingleErrorAtEndOfFile()
    {
        SyntaxTree tree = SyntaxTree.Parse("{ value = 1;");

        Diagnostic error = Assert.Single(tree.Diagnostics);
        Assert.Equal("TS1004", error.Code);
        Assert.Equal(TokenKind.EndOfFile, tree.Root.EndOfFile.Kind);
    }

    [Fact]
    public void Diagnostic_Format_IsTerminalFriendlyAndShowsSourceLocation()
    {
        const string source = "Point(1) = {0}\nLine(1) = {1, 2};";
        Diagnostic diagnostic = Assert.Single(SyntaxTree.Parse(source).Diagnostics);

        string formatted = diagnostic.Format(source, "mesh.geo");

        Assert.Contains("mesh.geo:2:1: error TS1006", formatted);
        Assert.Contains(" 2 | Line(1) = {1, 2};", formatted);
        Assert.Contains(" | ^", formatted);
    }

    [Fact]
    public void DiagnosticBag_StoresInfosWarningsAndErrors()
    {
        var diagnostics = new DiagnosticBag();
        var span = new TextSpan(0, 1, 1, 1);

        diagnostics.Info("TS0001", "information", span);
        diagnostics.Warning("TS0002", "warning", span);
        diagnostics.Error("TS0003", "error", span);

        Assert.Equal(
            [DiagnosticSeverity.Info, DiagnosticSeverity.Warning, DiagnosticSeverity.Error],
            diagnostics.Items.Select(item => item.Severity));
        Assert.True(diagnostics.HasErrors);
    }

    [Fact]
    public void Parse_GmshConditional_CreatesOrderedBranches()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "If (value > 10)\n" +
            "  result = 1;\n" +
            "ElseIf (value > 5)\n" +
            "  result = 2;\n" +
            "Else\n" +
            "  result = 3;\n" +
            "EndIf");

        var statement = Assert.IsType<IfStmt>(Assert.Single(tree.Root.Statements));
        Assert.DoesNotContain(tree.Diagnostics, item => item.Severity == DiagnosticSeverity.Error);
        Assert.Equal(3, statement.Branches.Count);
        Assert.NotNull(statement.Branches[0].Condition);
        Assert.NotNull(statement.Branches[1].Condition);
        Assert.Null(statement.Branches[2].Condition);
        Assert.All(statement.Branches, branch => Assert.Single(branch.Statements));
        Assert.Equal(KeywordKind.EndIf, statement.EndIfKeyword.Keyword);
    }

    [Fact]
    public void Parse_NestedConditionals_MatchesEachEndIf()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "If (1)\n" +
            "  If (0)\n" +
            "    value = 1;\n" +
            "  EndIf\n" +
            "EndIf");

        var outer = Assert.IsType<IfStmt>(Assert.Single(tree.Root.Statements));
        Assert.IsType<IfStmt>(Assert.Single(outer.Branches[0].Statements));
        Assert.DoesNotContain(tree.Diagnostics, item => item.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Parse_ConditionalWithoutEndIf_ReportsDiagnostic()
    {
        Diagnostic diagnostic = Assert.Single(SyntaxTree.Parse("If (1)\nvalue = 2;").Diagnostics);

        Assert.Equal("TS1008", diagnostic.Code);
    }

    [Theory]
    [InlineData("For (1:3)\nPrint(1);\nEndFor", false, false)]
    [InlineData("For i In {1:5:2}\nPrint(i);\nEndFor", true, true)]
    public void Parse_GmshLoop_CreatesForStatement(
        string source,
        bool hasIterator,
        bool hasStep)
    {
        SyntaxTree tree = SyntaxTree.Parse(source);

        var statement = Assert.IsType<ForStmt>(Assert.Single(tree.Root.Statements));
        Assert.Empty(tree.Diagnostics);
        Assert.Equal(hasIterator, statement.Iterator is not null);
        Assert.Equal(hasStep, statement.Step is not null);
        Assert.Null(statement.Items);
        Assert.Single(statement.Statements);
        Assert.Equal(KeywordKind.EndFor, statement.EndForKeyword.Keyword);
    }

    [Fact]
    public void Parse_ForInExplicitList_StoresEachItem()
    {
        SyntaxTree tree = SyntaxTree.Parse("For item In { 1, 2, 3 }\nPrint(item);\nEndFor");

        var statement = Assert.IsType<ForStmt>(Assert.Single(tree.Root.Statements));
        Assert.Empty(tree.Diagnostics);
        Assert.Equal(3, statement.Items?.Count);
        Assert.Null(statement.Start);
        Assert.Null(statement.End);
    }

    [Fact]
    public void Parse_NestedLoops_MatchesEachEndFor()
    {
        SyntaxTree tree = SyntaxTree.Parse(
            "For i In {1:2}\n" +
            "  For j In {1:2}\n" +
            "    Print(i + j);\n" +
            "  EndFor\n" +
            "EndFor");

        var outer = Assert.IsType<ForStmt>(Assert.Single(tree.Root.Statements));
        Assert.IsType<ForStmt>(Assert.Single(outer.Statements));
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_LoopWithoutEndFor_ReportsDiagnostic()
    {
        Diagnostic diagnostic = Assert.Single(SyntaxTree.Parse("For (1:2)\nPrint(1);").Diagnostics);

        Assert.Equal("TS1016", diagnostic.Code);
    }

    [Fact]
    public void Parse_ListIndexing_CreatesChainedIndexExpressions()
    {
        SyntaxTree tree = SyntaxTree.Parse("matrix[1][2];");

        var statement = Assert.IsType<ExpressionStmt>(Assert.Single(tree.Root.Statements));
        var outer = Assert.IsType<IndexExpr>(statement.Expression);
        Assert.IsType<IndexExpr>(outer.Target);
        Assert.IsType<LiteralExpr>(outer.Index);
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_MultiIndexSelection_UsesListAsIndex()
    {
        SyntaxTree tree = SyntaxTree.Parse("values[{0, 2}];");

        var statement = Assert.IsType<ExpressionStmt>(Assert.Single(tree.Root.Statements));
        var index = Assert.IsType<IndexExpr>(statement.Expression);
        Assert.IsType<ListExpr>(index.Index);
        Assert.Empty(tree.Diagnostics);
    }

    [Theory]
    [InlineData("Transfinite Curve{1} = 11;")]
    [InlineData("Transfinite Curve{All} = 11;")]
    [InlineData("Transfinite Curve{-1, 3} = count Using Progression 1.2;")]
    [InlineData("Transfinite Curve{1, 2} = 20 Using Bump 0.25;")]
    public void Parse_TransfiniteCurve_CreatesDedicatedStatement(string source)
    {
        SyntaxTree tree = SyntaxTree.Parse(source);

        Assert.IsType<TransfiniteCurveStmt>(Assert.Single(tree.Root.Statements));
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_TransfiniteCurveWithUnknownDistribution_ReportsFocusedDiagnostic()
    {
        Diagnostic diagnostic = Assert.Single(
            SyntaxTree.Parse("Transfinite Curve{1} = 11 Using Beta 2;").Diagnostics);

        Assert.Equal("TS1021", diagnostic.Code);
    }

    [Theory]
    [InlineData("Line Loop(1) = {1, 2, -3};")]
    [InlineData("Line loop(1) = {1, 2, -3};")]
    [InlineData("Curve Loop(1) = {1, 2, -3};")]
    public void Parse_CurveLoop_SupportsGmshNamesAndLegacyLineLoop(string source)
    {
        SyntaxTree tree = SyntaxTree.Parse(source);

        Assert.IsType<CurveLoopStmt>(Assert.Single(tree.Root.Statements));
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_PlaneSurface_CreatesDedicatedStatement()
    {
        SyntaxTree tree = SyntaxTree.Parse("Plane Surface(1) = {1, 2};");

        var statement = Assert.IsType<PlaneSurfaceStmt>(Assert.Single(tree.Root.Statements));
        Assert.Equal(2, statement.CurveLoops.Items.Count);
        Assert.Empty(tree.Diagnostics);
    }

    [Theory]
    [InlineData("Line {18, 19} In Surface {1};")]
    [InlineData("Curve {18, 19} In Surface {1};")]
    public void Parse_CurvesInSurface_CreatesEmbeddingStatement(string source)
    {
        SyntaxTree tree = SyntaxTree.Parse(source);

        var statement = Assert.IsType<CurvesInSurfaceStmt>(Assert.Single(tree.Root.Statements));
        Assert.Equal(2, statement.Curves.Items.Count);
        Assert.Single(statement.Surfaces.Items);
        Assert.Empty(tree.Diagnostics);
    }
}
