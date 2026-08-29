namespace TriUgla.Script.Playground.ViewModels;

public sealed class ScriptEditorViewModel
{
    IReadOnlyList<Token> _tokens = [];
    IReadOnlySet<int> _functionTokenStarts = new HashSet<int>();

    public string Source { get; private set; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; private set; } = [];
    public int LineCount => Math.Max(1, Source.Count(character => character == '\n') + 1);
    public int ErrorCount => Diagnostics.Count(item => item.Severity == DiagnosticSeverity.Error);
    public int WarningCount => Diagnostics.Count(item => item.Severity == DiagnosticSeverity.Warning);
    public int InfoCount => Diagnostics.Count(item => item.Severity == DiagnosticSeverity.Info);
    public IReadOnlyList<HighlightSegment> Highlights => BuildHighlights();
    public IReadOnlyList<HoverDocumentation> HoverDocumentation => BuildHoverDocumentation();

    public ScriptEditorViewModel(string source)
    {
        Source = source;
        Analyze();
    }

    public SyntaxTree Update(string source)
    {
        Source = source;
        return Analyze();
    }

    public SyntaxTree Analyze()
    {
        SyntaxTree tree = SyntaxTree.Parse(Source);
        Diagnostics = tree.Diagnostics;
        _tokens = new Tokenizer(Source).Tokenize();
        _functionTokenStarts = _tokens
            .Take(_tokens.Count - 1)
            .Select((token, index) => (token, index))
            .Where(item =>
                item.token.Kind is TokenKind.Identifier or TokenKind.Keyword &&
                _tokens[item.index + 1].Kind == TokenKind.LeftParenthesis)
            .Select(item => item.token.Span.Start)
            .ToHashSet();
        return tree;
    }

    IReadOnlyList<HighlightSegment> BuildHighlights()
    {
        if (Source.Length == 0) return [];

        var boundaries = new SortedSet<int> { 0, Source.Length };
        foreach (Token token in _tokens.Where(item => item.Kind != TokenKind.EndOfFile))
        {
            boundaries.Add(Math.Clamp(token.Span.Start, 0, Source.Length));
            boundaries.Add(Math.Clamp(token.Span.End, 0, Source.Length));
        }
        foreach (Diagnostic diagnostic in Diagnostics)
        {
            int start = Math.Clamp(diagnostic.Span.Start, 0, Source.Length - 1);
            boundaries.Add(start);
            boundaries.Add(Math.Clamp(start + Math.Max(1, diagnostic.Span.Length), start + 1, Source.Length));
        }

        int[] positions = boundaries.ToArray();
        var segments = new List<HighlightSegment>(positions.Length - 1);
        for (int index = 0; index < positions.Length - 1; index++)
        {
            int start = positions[index];
            int end = positions[index + 1];
            Token token = _tokens.FirstOrDefault(item =>
                item.Kind != TokenKind.EndOfFile && start >= item.Span.Start && start < item.Span.End);
            DiagnosticSeverity? severity = Diagnostics
                .Where(item => Overlaps(item.Span, start, end))
                .Select(item => (DiagnosticSeverity?)item.Severity)
                .OrderByDescending(item => item)
                .FirstOrDefault();
            string diagnosticClass = severity switch
            {
                DiagnosticSeverity.Error => " token-error",
                DiagnosticSeverity.Warning => " token-warning",
                DiagnosticSeverity.Info => " token-info",
                _ => string.Empty
            };
            segments.Add(new HighlightSegment(Source[start..end], TokenClass(token) + diagnosticClass));
        }
        return segments;
    }

    bool Overlaps(TextSpan span, int start, int end)
    {
        int diagnosticStart = Math.Clamp(span.Start, 0, Source.Length - 1);
        int diagnosticEnd = Math.Min(Source.Length, diagnosticStart + Math.Max(1, span.Length));
        return start < diagnosticEnd && end > diagnosticStart;
    }

    string TokenClass(Token token) => token.Kind switch
    {
        _ when _functionTokenStarts.Contains(token.Span.Start) => "token-function",
        TokenKind.Keyword => "token-keyword",
        TokenKind.Number => "token-number",
        TokenKind.String => "token-string",
        TokenKind.Identifier => "token-identifier",
        TokenKind.BadToken => "token-bad",
        TokenKind.EndOfFile => "token-plain",
        _ => "token-symbol"
    };

    IReadOnlyList<HoverDocumentation> BuildHoverDocumentation()
    {
        var items = new List<HoverDocumentation>();
        for (int index = 0; index < _tokens.Count; index++)
        {
            Token token = _tokens[index];
            if (token.Kind is TokenKind.EndOfFile or TokenKind.BadToken) continue;
            string key = index >= 2 && _tokens[index - 1].Kind == TokenKind.Dot
                ? $"{_tokens[index - 2].Text}.{token.Text}"
                : token.Text;
            if (!ScriptDocumentation.TryGet(key, out ScriptDocumentationEntry? documentation) &&
                !TryGetGenericDocumentation(token, out documentation)) continue;
            items.Add(new HoverDocumentation(
                token.Span.Start,
                token.Span.Length,
                documentation.Name,
                documentation.Signature,
                documentation.Description,
                documentation.AcceptedValues));
        }
        return items;
    }

    static bool TryGetGenericDocumentation(Token token, out ScriptDocumentationEntry documentation)
    {
        string key = token.Kind switch
        {
            TokenKind.LeftBracket or TokenKind.RightBracket => "[]",
            TokenKind.LeftBrace or TokenKind.RightBrace => "{}",
            _ => token.Text
        };
        if (ScriptDocumentation.TryGet(key, out documentation!)) return true;
        documentation = token.Kind switch
        {
            TokenKind.Number => new("Number literal", token.Text, "A double-precision numeric value. Decimal and scientific notation are supported."),
            TokenKind.String => new("String literal", token.Text, "Text enclosed in double quotes. Escaped quotes and backslashes are supported."),
            TokenKind.Identifier => new("Identifier", token.Text, "A case-sensitive name resolved from the current lexical scope."),
            _ => null!
        };
        return documentation is not null;
    }
}

public sealed record HighlightSegment(string Text, string CssClass);

public sealed record HoverDocumentation(
    int Start,
    int Length,
    string Name,
    string Signature,
    string Description,
    string? AcceptedValues);
