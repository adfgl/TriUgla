namespace TriUgla.Script;

public readonly record struct Token(
    TokenKind Kind,
    string Text,
    TextSpan Span,
    KeywordKind Keyword = KeywordKind.None);
