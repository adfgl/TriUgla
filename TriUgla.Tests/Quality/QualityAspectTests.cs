namespace TriUgla.Tests;

public class QualityAspectTests
{
    readonly Face _face = QualityTestMesh.Triangle(new(0, 0), new(1, 0), new(0, 1));

    [Fact]
    public void AreaAspectReportsLargestBoundViolation()
    {
        AreaAspect aspect = new() { MinArea = 1, MaxArea = 4 };

        Assert.Equal(0.5, aspect.Violation01(_face, new FaceStats(0.5, 1, 2, 0, 0, 0)), 12);
        Assert.Equal(0.5, aspect.Violation01(_face, new FaceStats(6, 1, 2, 0, 0, 0)), 12);
        Assert.Equal(0, aspect.Violation01(_face, new FaceStats(2, 1, 2, 0, 0, 0)), 12);
    }

    [Fact]
    public void EdgeLengthAspectUsesActualLengths()
    {
        EdgeLengthAspect aspect = new() { MinEdgeLength = 2, MaxEdgeLength = 3 };
        FaceStats stats = new(1, 1, 16, 0, 0, 0);

        Assert.Equal(0.5, aspect.Violation01(_face, in stats), 12);
    }

    [Fact]
    public void VertexAreaAspectHonorsTolerance()
    {
        VertexAreaAspect aspect = new() { OverTolerance = 0.25 };
        FaceStats stats = new(2.5, 1, 2, 1, 0, 0);

        Assert.Equal(1, aspect.Violation01(_face, in stats), 12);
    }

    [Fact]
    public void AngleAspectAcceptsEquilateralTriangleAtSixtyDegrees()
    {
        Face face = QualityTestMesh.Triangle(
            new(0, 0), new(1, 0), new(0.5, Math.Sqrt(3) / 2));
        new DefaultFaceStatsCollector().TryCollect(face, out FaceStats stats);
        AngleAspect aspect = new() { MinAngleDeg = 60 };

        Assert.Equal(0, aspect.Violation01(face, in stats), 12);
    }

    [Fact]
    public void AngleAspectPenalizesNeedleTriangle()
    {
        Face face = QualityTestMesh.Triangle(new(0, 0), new(10, 0), new(0.01, 0.01));
        new DefaultFaceStatsCollector().TryCollect(face, out FaceStats stats);
        AngleAspect aspect = new() { MinAngleDeg = 20 };

        Assert.True(aspect.Violation01(face, in stats) > 0.9);
    }
}
