namespace TriUgla.Script;

public sealed class DiagnosticBag
{
    readonly List<Diagnostic> _diagnostics = [];

    public IReadOnlyList<Diagnostic> Items => _diagnostics;
    public bool HasErrors => _diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error);

    public void Clear() => _diagnostics.Clear();

    public void Info(string code, string message, TextSpan span)
        => Add(DiagnosticSeverity.Info, code, message, span);

    public void Warning(string code, string message, TextSpan span)
        => Add(DiagnosticSeverity.Warning, code, message, span);

    public void Error(string code, string message, TextSpan span)
        => Add(DiagnosticSeverity.Error, code, message, span);

    public void Add(DiagnosticSeverity severity, string code, string message, TextSpan span)
        => _diagnostics.Add(new Diagnostic(severity, code, message, span));
}
