namespace TriUgla.Script
{
    public readonly record struct Token(
      TokenKind Kind,
      Position Position,
      Span Span,
      int Value = 0);
}
