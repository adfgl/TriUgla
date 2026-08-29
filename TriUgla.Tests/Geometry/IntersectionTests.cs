namespace TriUgla.Tests;

public class IntersectionTests
{
    [Fact]
    public void Intersect_CrossingSegments_ReturnsIntersection()
    {
        bool intersects = Intersection.Intersect(
            new Vec2(0, 0), new Vec2(2, 2),
            new Vec2(0, 2), new Vec2(2, 0),
            out Vec2 intersection);

        Assert.True(intersects);
        Assert.Equal(new Vec2(1, 1), intersection);
    }

    [Fact]
    public void Intersect_SharedEndpoint_ReturnsEndpoint()
    {
        bool intersects = Intersection.Intersect(
            new Vec2(0, 0), new Vec2(1, 1),
            new Vec2(1, 1), new Vec2(2, 0),
            out Vec2 intersection);

        Assert.True(intersects);
        Assert.Equal(new Vec2(1, 1), intersection);
    }

    [Fact]
    public void Intersect_LinesCrossOutsideSegments_ReturnsFalse()
    {
        bool intersects = Intersection.Intersect(
            new Vec2(0, 0), new Vec2(1, 0),
            new Vec2(2, -1), new Vec2(2, 1),
            out Vec2 intersection);

        Assert.False(intersects);
        Assert.True(double.IsNaN(intersection.X));
        Assert.True(double.IsNaN(intersection.Y));
    }

    [Fact]
    public void Intersect_ParallelSegments_ReturnsFalse()
    {
        bool intersects = Intersection.Intersect(
            new Vec2(0, 0), new Vec2(2, 0),
            new Vec2(0, 1), new Vec2(2, 1),
            out _);

        Assert.False(intersects);
    }

    [Fact]
    public void Intersect_CollinearSegments_ReturnsFalse()
    {
        bool intersects = Intersection.Intersect(
            new Vec2(0, 0), new Vec2(2, 0),
            new Vec2(1, 0), new Vec2(3, 0),
            out _);

        Assert.False(intersects);
    }

    [Fact]
    public void Intersect_DegenerateSegment_ReturnsFalse()
    {
        bool intersects = Intersection.Intersect(
            Vec2.Zero, Vec2.Zero,
            new Vec2(-1, 0), new Vec2(1, 0),
            out _);

        Assert.False(intersects);
    }

    [Fact]
    public void Intersect_NegativeEpsilon_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Intersection.Intersect(
            Vec2.Zero, Vec2.UnitX,
            Vec2.Zero, Vec2.UnitY,
            out _,
            parallelEpsilon: -1));
}
