using TriUgla;

namespace TriUgla.Tests;

public class GeometryPredicatesTests
{
    readonly GeometryPredicates _geometry = new();

    [Theory]
    [InlineData(0, 1, EOrientaiton.Counterclockwise)]
    [InlineData(0, -1, EOrientaiton.Clockwise)]
    [InlineData(1, 0, EOrientaiton.Collinear)]
    public void Orient_ClassifiesPointRelativeToDirectedEdge(
        double pointX,
        double pointY,
        EOrientaiton expected)
    {
        Node start = NodeAt(0, 0);
        Node end = NodeAt(2, 0);

        Assert.Equal(expected, _geometry.Orient(start, end, new Vec2(pointX, pointY)));
    }

    [Fact]
    public void Orient_EdgeOverloadUsesItsDirectedEndpoints()
    {
        Node start = NodeAt(0, 0);
        Node end = NodeAt(2, 0);
        var edge = new Edge { NodeStart = start };
        edge.Next = new Edge { NodeStart = end };

        Assert.Equal(
            EOrientaiton.Counterclockwise,
            _geometry.Orient(edge, new Vec2(1, 1)));
    }

    [Fact]
    public void Orient_NearCollinearLargeCoordinatesUsesExactFallback()
    {
        double perturbation = Math.BitIncrement(2e20) - 2e20;

        int sign = _geometry.OrientSign(
            Vec2.Zero,
            new Vec2(1e20, 1e20),
            new Vec2(2e20, 2e20 + perturbation));

        Assert.Equal(1, sign);
        Assert.True(_geometry.ExactOrientationComputations > 0);
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, false)]
    public void InDiameterCircle_UsesStrictInterior(
        double pointX,
        double pointY,
        bool expected)
    {
        Assert.Equal(expected, _geometry.InDiameterCircle(
            NodeAt(0, 0),
            NodeAt(2, 0),
            new Vec2(pointX, pointY)));
    }

    [Theory]
    [InlineData(.5, .5, true)]
    [InlineData(1, 1, false)]
    [InlineData(2, 2, false)]
    public void InCircumcircle_UsesStrictInterior(
        double pointX,
        double pointY,
        bool expected)
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(1, 0);
        Node c = NodeAt(0, 1);

        Assert.Equal(expected, _geometry.InCircumcircle(a, b, c, new Vec2(pointX, pointY)));
        Assert.Equal(expected, _geometry.InCircumcircle(c, b, a, new Vec2(pointX, pointY)));
    }

    [Fact]
    public void InCircumcircle_DegenerateTriangleReturnsFalse()
        => Assert.False(_geometry.InCircumcircle(
            NodeAt(0, 0),
            NodeAt(1, 0),
            NodeAt(2, 0),
            new Vec2(1, .1)));

    [Fact]
    public void IsConvexQuad_AcceptsEitherOrientationAndRejectsConcavity()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(2, 0);
        Node c = NodeAt(2, 2);
        Node d = NodeAt(0, 2);

        Assert.True(_geometry.IsConvexQuad(new Quad(a, b, c, d)));
        Assert.True(_geometry.IsConvexQuad(new Quad(d, c, b, a)));
        Assert.False(_geometry.IsConvexQuad(new Quad(a, b, NodeAt(1, .5), d)));
        Assert.False(_geometry.IsConvexQuad(new Quad(a, b, NodeAt(3, 0), d)));
    }

    [Fact]
    public void Predicates_RejectNonFiniteCoordinates()
        => Assert.Throws<ArgumentException>(() =>
            _geometry.OrientSign(Vec2.Zero, Vec2.UnitX, new Vec2(double.NaN, 0)));

    [Fact]
    public void ExactMathCanBeDisabled()
    {
        var predicates = new GeometryPredicates { AllowExactMath = false };

        predicates.OrientSign(Vec2.Zero, Vec2.UnitX, Vec2.UnitX);

        Assert.Equal(0, predicates.ExactOrientationComputations);
    }

    [Theory]
    [InlineData(0, 0, 2, 2, 0, 2, 2, 0, 1)]
    [InlineData(0, 0, 1, 0, 1, 0, 2, 1, 0)]
    [InlineData(0, 0, 2, 0, 1, 0, 3, 0, 2)]
    [InlineData(0, 0, 1, 0, 2, 0, 3, 0, -1)]
    public void Intersects_ClassifiesSegmentRelationships(
        double p1x,
        double p1y,
        double p2x,
        double p2y,
        double q1x,
        double q1y,
        double q2x,
        double q2y,
        int expected)
        => Assert.Equal(expected, _geometry.Intersects(
            new Vec2(p1x, p1y),
            new Vec2(p2x, p2y),
            new Vec2(q1x, q1y),
            new Vec2(q2x, q2y)));

    static Node NodeAt(double x, double y) => new() { Position = new Vec2(x, y) };
}
