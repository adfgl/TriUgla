namespace TriUgla.Script;

public readonly record struct Diagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    TextSpan Span)
{
    public string Format(string source, string? fileName = null)
    {
        string location = fileName is null
            ? $"{Span.Line}:{Span.Column}"
            : $"{fileName}:{Span.Line}:{Span.Column}";
        string line = GetLine(source, Span.Line);
        string gutter = Span.Line.ToString();
        int caretLength = Math.Max(1, Math.Min(Span.Length, Math.Max(1, line.Length - Span.Column + 1)));

        return $"{location}: {Severity.ToString().ToLowerInvariant()} {Code}: {Message}" +
               Environment.NewLine +
               $" {gutter} | {line}" + Environment.NewLine +
               $" {new string(' ', gutter.Length)} | {new string(' ', Math.Max(0, Span.Column - 1))}" +
               new string('^', caretLength);
    }

    static string GetLine(string source, int lineNumber)
    {
        using var reader = new StringReader(source);
        for (int line = 1; line < lineNumber; line++)
        {
            if (reader.ReadLine() is null)
            {
                return string.Empty;
            }
        }

        return reader.ReadLine() ?? string.Empty;
    }
}
