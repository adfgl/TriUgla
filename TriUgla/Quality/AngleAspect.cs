namespace TriUgla;

public sealed class AngleAspect : FaceRankAspectBase
{
    public double MinAngleDeg { get; set; }
    public double AngleRatioThreshold { get; set; }

    public override double Violation01(Face face, in FaceStats stats)
    {
        if (stats.MinLen2 <= Epsilon) return 0d;

        Edge edge = face.Edge;
        double radius2 = Circle.From3(
            edge.NodeStart.Position,
            edge.NodeEnd.Position,
            edge.Next.NodeEnd.Position).RadiusSquared;
        if (radius2 <= 0d || !double.IsFinite(radius2)) return 0d;

        double ratio = Math.Sqrt(radius2 / stats.MinLen2);
        double threshold = AngleRatioThreshold > 0d
            ? AngleRatioThreshold
            : MinAngleDeg > 0d ? RatioFromMinimumAngle(MinAngleDeg) : 0d;

        if (threshold <= 0d || !double.IsFinite(threshold) || ratio <= threshold)
            return 0d;

        return Clamp01(SafeDivide(ratio - threshold, threshold));
    }

    static double RatioFromMinimumAngle(double degrees)
        => 1d / (2d * Math.Sin(degrees * Math.PI / 180d));
}
