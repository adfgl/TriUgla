namespace TriUgla.Tests;

public class DefaultFaceStatsCollectorTests
{
    [Fact]
    public void CollectsGeometryAndTargetArea()
    {
        Face face = QualityTestMesh.Triangle(
            new Vec2(0, 0, 3),
            new Vec2(4, 0, 6),
            new Vec2(0, 3, 9));

        bool collected = new DefaultFaceStatsCollector().TryCollect(face, out FaceStats stats);

        Assert.True(collected);
        Assert.Equal(6, stats.SignedArea, 12);
        Assert.Equal(9, stats.MinLen2, 12);
        Assert.Equal(25, stats.MaxLen2, 12);
        Assert.Equal(6, stats.AvgVertexArea, 12);
        Assert.Equal(4d / 3d, stats.Cx, 12);
        Assert.Equal(1, stats.Cy, 12);
    }

    [Fact]
    public void ReturnsFalseForUnlinkedFace()
    {
        Assert.False(new DefaultFaceStatsCollector().TryCollect(new Face(), out _));
    }

}
