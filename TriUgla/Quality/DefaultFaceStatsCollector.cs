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

        double signedAreaTwice = 0d;
        double minLen2 = double.PositiveInfinity;
        double maxLen2 = 0d;
        double vertexAreaSum = 0d;
        double xSum = 0d;
        double ySum = 0d;
        int count = 0;

        foreach (Edge edge in face.Edges)
        {
            Vec2 start = edge.NodeStart.Position;
            Vec2 end = edge.NodeEnd.Position;
            double len2 = edge.LengthSquared;

            signedAreaTwice += start.Cross(end);
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
            signedAreaTwice * 0.5d,
            minLen2,
            maxLen2,
            vertexAreaSum / count,
            xSum / count,
            ySum / count);
        return true;
    }
}
