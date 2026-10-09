namespace TriUgla;

public sealed class Constraints
{
    public List<ConstraintPoint> Points { get; } = [];
    public List<ConstraintLine> Lines { get; } = [];
    public List<Polyline> Polylines { get; } = [];
    public List<Loop> Loops { get; } = [];
}
