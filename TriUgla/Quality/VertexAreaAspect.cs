namespace TriUgla;

public sealed class VertexAreaAspect : FaceRankAspectBase
{
    public double OverTolerance { get; set; }

    public override double Violation01(Face face, in FaceStats stats)
    {
        double faceArea = Math.Abs(stats.SignedArea);
        double targetArea = stats.AvgVertexArea;
        if (faceArea <= Epsilon || targetArea <= Epsilon) return 0d;

        double allowed = targetArea * (1d + Math.Max(0d, OverTolerance));
        if (faceArea <= allowed) return 0d;

        return Clamp01(SafeDivide(faceArea - allowed, allowed));
    }
}
