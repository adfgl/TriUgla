namespace TriUgla;

public sealed class AreaAspect : FaceRankAspectBase
{
    public double MinArea { get; set; }
    public double MaxArea { get; set; } = double.PositiveInfinity;

    public override double Violation01(Face face, in FaceStats stats)
    {
        double area = Math.Abs(stats.SignedArea);
        if (area <= Epsilon) return 0d;

        double belowMinimum = MinArea > 0d && area < MinArea
            ? Clamp01(SafeDivide(MinArea - area, MinArea))
            : 0d;
        double aboveMaximum = !double.IsPositiveInfinity(MaxArea) && area > MaxArea
            ? Clamp01(SafeDivide(area - MaxArea, MaxArea))
            : 0d;

        return Math.Max(belowMinimum, aboveMaximum);
    }
}
