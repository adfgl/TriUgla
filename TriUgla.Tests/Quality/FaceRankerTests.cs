namespace TriUgla.Tests;

public class FaceRankerTests
{
    [Fact]
    public void RankIsZeroWhenAllAspectsAreDisabled()
    {
        FaceRanker ranker = new();
        ranker.Angle.Weight = 0;
        ranker.VertexArea.Weight = 0;

        Assert.Equal(0, ranker.Rank(QualityTestMesh.Triangle(new(0, 0), new(1, 0), new(0, 1))));
    }

    [Fact]
    public void RankUsesWorstWeightedViolation()
    {
        FaceRanker ranker = new();
        ranker.Angle.Weight = 0;
        ranker.VertexArea.Weight = 0;
        ranker.Area.Weight = 2;
        ranker.Area.MaxArea = 0.25;
        ranker.Edge.Weight = 1;
        ranker.Edge.MaxEdgeLength = 1;

        double rank = ranker.Rank(
            QualityTestMesh.Triangle(new(0, 0), new(1, 0), new(0, 1)));

        Assert.Equal(1, rank, 12);
    }

    [Fact]
    public void ReturnsZeroWhenCollectorCannotCollect()
    {
        FaceRanker ranker = new() { Collector = new RejectingCollector() };

        Assert.Equal(0, ranker.Rank(new Face()));
    }

    sealed class RejectingCollector : IFaceStatsCollector
    {
        public bool TryCollect(Face face, out FaceStats stats)
        {
            stats = default;
            return false;
        }
    }
}
