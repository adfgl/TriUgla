using TriUgla.Script;

namespace TriUgla.Tests;

public class SemanticAnalyzerTests
{
    [Fact]
    public void Parse_UnusedVariable_ReportsWarningAtDeclaration()
    {
        SyntaxTree tree = SyntaxTree.Parse("unused = 42;");

        Diagnostic warning = Assert.Single(tree.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("TS2001", warning.Code);
        Assert.Equal("unused", tree.Source.Substring(warning.Span.Start, warning.Span.Length));
        Assert.False(tree.HasErrors);
    }

    [Fact]
    public void Parse_UsedVariable_DoesNotReportWarning()
    {
        SyntaxTree tree = SyntaxTree.Parse("value = 42; Print(value);");

        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_UnusedLoopIterator_ReportsWarning()
    {
        SyntaxTree tree = SyntaxTree.Parse("For item In {1:3}\nPrint(1);\nEndFor");

        Diagnostic warning = Assert.Single(tree.Diagnostics);
        Assert.Equal("item", tree.Source.Substring(warning.Span.Start, warning.Span.Length));
    }

    [Fact]
    public void Parse_ReadFromNestedScope_MarksOuterVariableUsed()
    {
        SyntaxTree tree = SyntaxTree.Parse("value = 1; { Print(value); }");

        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_PrimitiveDeclarations_DoNotCreateVariableWarnings()
    {
        SyntaxTree tree = SyntaxTree.Parse("Point(1) = {0, 0, 0};");

        Assert.Empty(tree.Diagnostics);
    }
}
