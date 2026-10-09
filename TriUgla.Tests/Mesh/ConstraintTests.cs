using TriUgla;

namespace TriUgla.Tests;

public class ConstraintTests
{
    [Fact]
    public void Constraints_StartWithEmptyCollections()
    {
        var constraints = new Constraints();

        Assert.Empty(constraints.Points);
        Assert.Empty(constraints.Lines);
        Assert.Empty(constraints.Polylines);
        Assert.Empty(constraints.Loops);
    }

    [Fact]
    public void ConstraintPointAndSpan_RejectNullNodes()
    {
        Assert.Throws<ArgumentNullException>(() => new ConstraintPoint(null!));
        Assert.Throws<ArgumentNullException>(() => new ConstraintLine(null!, new Node()));
        Assert.Throws<ArgumentNullException>(() => new ConstraintLine(new Node(), null!));
    }

    [Fact]
    public void Edges_FollowsAlignedChainToDestination()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(1, .02);
        Node c = NodeAt(2, 0);
        Edge ab = DirectedEdge(a, b);
        Edge bc = DirectedEdge(b, c);
        a.Edge = ab;
        b.Edge = bc;
        var span = new ConstraintLine(a, c);

        List<Edge> result = span.Edges([]);

        Assert.Equal([ab, bc], result);
    }

    [Fact]
    public void Edges_RotatesAroundNodeToFindAlignedEdge()
    {
        Node start = NodeAt(0, 0);
        Node wrongEnd = NodeAt(0, 1);
        Node target = NodeAt(2, 0);
        Edge wrong = DirectedEdge(start, wrongEnd);
        Edge aligned = DirectedEdge(start, target);
        var incoming = new Edge { Twin = aligned };
        wrong.Prev = incoming;
        start.Edge = wrong;

        List<Edge> result = new ConstraintLine(start, target).Edges([]);

        Assert.Equal([aligned], result);
    }

    [Fact]
    public void ConstraintLine_RejectsSameNode()
    {
        Node node = NodeAt(0, 0);

        Assert.Throws<ArgumentException>(() => new ConstraintLine(node, node));
    }

    [Fact]
    public void ConstraintLine_RejectsSettingEitherEndpointToTheOther()
    {
        Node from = NodeAt(0, 0);
        Node to = NodeAt(1, 0);
        var span = new ConstraintLine(from, to);

        Assert.Throws<ArgumentException>(() => span.From = to);
        Assert.Throws<ArgumentException>(() => span.To = from);
        Assert.Same(from, span.From);
        Assert.Same(to, span.To);
    }

    [Fact]
    public void Edges_ThrowsWhenNoAlignedPathExists()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(0, 1);
        Node target = NodeAt(2, 0);
        a.Edge = DirectedEdge(a, b);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new ConstraintLine(a, target).Edges([]));

        Assert.Contains("no aligned outgoing edge", exception.Message);
    }

    [Fact]
    public void Edges_DoesNotFollowAlignedEdgePastDestination()
    {
        Node start = NodeAt(0, 0);
        Node target = NodeAt(1, 0);
        Node pastTarget = NodeAt(2, 0);
        Edge overshooting = DirectedEdge(start, pastTarget);
        Edge destination = DirectedEdge(start, target);
        var incoming = new Edge { Twin = destination };
        overshooting.Prev = incoming;
        start.Edge = overshooting;

        List<Edge> result = new ConstraintLine(start, target).Edges([]);

        Assert.Equal([destination], result);
    }

    [Fact]
    public void Edges_ThrowsForDistinctCoincidentNodes()
    {
        Node a = NodeAt(1, 1);
        Node b = NodeAt(1, 1);

        Assert.Throws<InvalidOperationException>(() => new ConstraintLine(a, b).Edges([]));
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(.995, .1, true)]
    [InlineData(0, 1, false)]
    public void NearlyColliniear_UsesNormalizedDirectionDot(
        double x,
        double y,
        bool expected)
    {
        Edge edge = DirectedEdge(NodeAt(0, 0), NodeAt(x, y));

        Assert.Equal(expected, ConstraintLine.NearlyColliniear(Vec2.UnitX, edge));
    }

    static Edge DirectedEdge(Node start, Node end)
    {
        var edge = new Edge { NodeStart = start };
        edge.Next = new Edge { NodeStart = end };
        return edge;
    }

    static Node NodeAt(double x, double y) => new() { Position = new Vec2(x, y) };
}
