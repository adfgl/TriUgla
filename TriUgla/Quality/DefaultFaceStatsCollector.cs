namespace TriUgla;

public sealed class DefaultFaceStatsCollector : IFaceStatsCollector
{
    public bool TryCollect(Face face, out FaceStats stats)
    {
        ArgumentNullException.ThrowIfNull(face);

        if (face.Edge is null)
        {
            stats = default;
            return false;
        }

        double minLen2 = double.PositiveInfinity;
        double maxLen2 = 0d;
        double vertexAreaSum = 0d;
        double xSum = 0d;
        double ySum = 0d;
        int count = 0;

        foreach (Edge edge in face.Edges)
        {
            Vec2 start = edge.NodeStart.Position;
            double len2 = edge.LengthSquared;

            minLen2 = Math.Min(minLen2, len2);
            maxLen2 = Math.Max(maxLen2, len2);
            vertexAreaSum += edge.NodeStart.Data.Area;
            xSum += start.X;
            ySum += start.Y;
            count++;
        }

        if (count < 3)
        {
            stats = default;
            return false;
        }

        stats = new FaceStats(
            face.SignedArea,
            minLen2,
            maxLen2,
            vertexAreaSum / count,
            xSum / count,
            ySum / count);
        return true;
    }
}
