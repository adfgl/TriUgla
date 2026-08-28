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
}
