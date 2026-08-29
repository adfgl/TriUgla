namespace TriUgla.Script;

public sealed record SyntaxTree(
    string Source,
    CompilationUnit Root,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error);

    public static SyntaxTree Parse(string source)
    {
        var parser = new Parser(source);
        CompilationUnit root = parser.ParseCompilationUnit();
        IReadOnlyList<Diagnostic> diagnostics = parser.Diagnostics;
        if (!parser.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
        {
            diagnostics = parser.Diagnostics
                .Concat(new SemanticAnalyzer().Analyze(root))
                .OrderBy(item => item.Span.Start)
                .ToArray();
        }
        return new SyntaxTree(source, root, diagnostics);
    }
}
