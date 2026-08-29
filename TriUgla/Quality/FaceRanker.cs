namespace TriUgla;

public sealed class FaceRanker
{
    public IFaceStatsCollector Collector { get; set; } = new DefaultFaceStatsCollector();

    public AreaAspect Area { get; } = new() { Weight = 0d };
    public EdgeLengthAspect Edge { get; } = new() { Weight = 0d };
    public AngleAspect Angle { get; } = new() { Weight = 1d };
    public VertexAreaAspect VertexArea { get; } = new() { Weight = 1d };

    public double Rank(Face face)
    {
        ArgumentNullException.ThrowIfNull(face);
        if (!Collector.TryCollect(face, out FaceStats stats)) return 0d;

        double worst = 0d;
        double maxWeight = 0d;
        Accumulate(Area, face, in stats, ref worst, ref maxWeight);
        Accumulate(Edge, face, in stats, ref worst, ref maxWeight);
        Accumulate(Angle, face, in stats, ref worst, ref maxWeight);
        Accumulate(VertexArea, face, in stats, ref worst, ref maxWeight);

        return maxWeight <= 0d ? 0d : Math.Clamp(worst / maxWeight, 0d, 1d);
    }

    static void Accumulate(
        FaceRankAspect aspect,
        Face face,
        in FaceStats stats,
        ref double worst,
        ref double maxWeight)
    {
        if (!aspect.Enabled) return;

        worst = Math.Max(worst, aspect.Violation01(face, in stats) * aspect.Weight);
        maxWeight = Math.Max(maxWeight, aspect.Weight);
    }
}
