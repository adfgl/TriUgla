namespace TriUgla;

public sealed class EdgeLengthAspect : FaceRankAspectBase
{
    public double MinEdgeLength { get; set; }
    public double MaxEdgeLength { get; set; } = double.PositiveInfinity;

    public override double Violation01(Face face, in FaceStats stats)
    {
        double belowMinimum = 0d;
        if (MinEdgeLength > 0d && stats.MinLen2 < MinEdgeLength * MinEdgeLength)
        {
            double minLength = Math.Sqrt(Math.Max(stats.MinLen2, 0d));
            belowMinimum = Clamp01(SafeDivide(MinEdgeLength - minLength, MinEdgeLength));
        }

        double aboveMaximum = 0d;
        if (!double.IsPositiveInfinity(MaxEdgeLength) &&
            stats.MaxLen2 > MaxEdgeLength * MaxEdgeLength)
        {
            double maxLength = Math.Sqrt(Math.Max(stats.MaxLen2, 0d));
            aboveMaximum = Clamp01(SafeDivide(maxLength - MaxEdgeLength, MaxEdgeLength));
        }

        return Math.Max(belowMinimum, aboveMaximum);
    }
}
