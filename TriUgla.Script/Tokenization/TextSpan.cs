namespace TriUgla.Script;

public readonly record struct TextSpan(
    int Start,
    int Length,
    int Line,
    int Column)
{
    public int End => Start + Length;

    public static TextSpan FromBounds(TextSpan first, TextSpan last)
        => new(first.Start, last.End - first.Start, first.Line, first.Column);
}
