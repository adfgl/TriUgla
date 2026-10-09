namespace TriUgla;

public interface IConstraints
{
    IReadOnlyList<ConstraintPoint> Points { get; }
    IReadOnlyList<ConstraintLine> Lines { get; }
    IReadOnlyList<Polyline> Polylines { get; }
    IReadOnlyList<Loop> Loops { get; }
}

internal sealed class Constraints : IConstraints
{
    internal List<ConstraintPoint> Points { get; } = [];
    internal List<ConstraintLine> Lines { get; } = [];
    internal List<Polyline> Polylines { get; } = [];
    internal List<Loop> Loops { get; } = [];

    IReadOnlyList<ConstraintPoint> IConstraints.Points => Points;
    IReadOnlyList<ConstraintLine> IConstraints.Lines => Lines;
    IReadOnlyList<Polyline> IConstraints.Polylines => Polylines;
    IReadOnlyList<Loop> IConstraints.Loops => Loops;
}
