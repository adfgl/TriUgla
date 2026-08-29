namespace TriUgla;

public sealed class Constraint(
    IEnumerable<ConstraintPoint>? points = null,
    IEnumerable<ConstraintSpan>? spans = null,
    string? name = null) : INamable
{
    public string? Name { get; set; } = name;
    public List<ConstraintPoint> Points { get; set; } = points?.ToList() ?? [];
    public List<ConstraintSpan> Spans { get; set; } = spans?.ToList() ?? [];
}
