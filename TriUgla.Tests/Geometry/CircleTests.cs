namespace TriUgla.Tests;

public class CircleTests
{
    [Fact]
    public void From2CreatesDiameterCircle()
    {
        Circle circle = Circle.From2(new Vec2(0, 0), new Vec2(4, 0));

        Assert.Equal(new Vec2(2, 0), circle.Center);
        Assert.Equal(4, circle.RadiusSquared);
    }

    [Fact]
    public void From3CreatesCircumcircle()
    {
        Circle circle = Circle.From3(
            new Vec2(0, 0),
            new Vec2(4, 0),
            new Vec2(0, 3));

        Assert.Equal(new Vec2(2, 1.5), circle.Center);
        Assert.Equal(6.25, circle.RadiusSquared, 12);
    }

    [Fact]
    public void ContainsIsStrictAndExcludesBoundary()
    {
        Circle circle = Circle.From2(new Vec2(0, 0), new Vec2(4, 0));

        Assert.True(circle.Contains(new Vec2(2, 0)));
        Assert.False(circle.Contains(new Vec2(0, 0)));
        Assert.False(circle.Contains(new Vec2(5, 0)));
    }

}
