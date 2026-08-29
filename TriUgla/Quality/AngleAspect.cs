namespace TriUgla;

public sealed class AngleAspect : FaceRankAspectBase
{
    public double MinAngleDeg { get; set; }
    public double AngleRatioThreshold { get; set; }

    public override double Violation01(Face face, in FaceStats stats)
    {
        if (stats.MinLen2 <= Epsilon) return 0d;

        double radius2 = CircumradiusSquared(face, stats.SignedArea);
        if (radius2 <= 0d || !double.IsFinite(radius2)) return 0d;

        double ratio = Math.Sqrt(radius2 / stats.MinLen2);
        double threshold = AngleRatioThreshold > 0d
            ? AngleRatioThreshold
            : MinAngleDeg > 0d ? RatioFromMinimumAngle(MinAngleDeg) : 0d;

        if (threshold <= 0d || !double.IsFinite(threshold) || ratio <= threshold)
            return 0d;

        return Clamp01(SafeDivide(ratio - threshold, threshold));
    }

    static double CircumradiusSquared(Face face, double signedArea)
    {
        Edge first = face.Edge;
        Vec2 a = first.NodeStart.Position;
        Vec2 b = first.NodeEnd.Position;
        Vec2 c = first.Next.NodeEnd.Position;
        double ab2 = a.DistanceSquared(b);
        double bc2 = b.DistanceSquared(c);
        double ca2 = c.DistanceSquared(a);
        double area2 = signedArea * signedArea;
        return area2 <= Epsilon ? 0d : ab2 * bc2 * ca2 / (16d * area2);
    }

    static double RatioFromMinimumAngle(double degrees)
        => 1d / (2d * Math.Sin(degrees * Math.PI / 180d));
}
