namespace TriUgla.Script;

public readonly record struct TextSpan(
    int Start,
    int Length,
    int Line,
    int Column)
{
    public int End => Start + Length;
}
