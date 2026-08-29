using TriUgla;

namespace TriUgla.Tests;

public class ConstraintTests
{
    [Fact]
    public void Constraint_CopiesInitialCollectionsAndStoresNames()
    {
        var point = new ConstraintPoint(NodeAt(0, 0), "anchor");
        var span = new ConstraintSpan(NodeAt(0, 0), NodeAt(1, 0), "boundary");

        var constraint = new Constraint([point], [span], "profile");

        Assert.Equal("profile", constraint.Name);
        Assert.Same(point, Assert.Single(constraint.Points));
        Assert.Same(span, Assert.Single(constraint.Spans));
        Assert.Equal("anchor", point.Name);
        Assert.Equal("boundary", span.Name);
    }

    [Fact]
    public void Constraint_DefaultsToEmptyCollections()
    {
        var constraint = new Constraint();

        Assert.Empty(constraint.Points);
        Assert.Empty(constraint.Spans);
    }

    [Fact]
    public void ConstraintPointAndSpan_RejectNullNodes()
    {
        Assert.Throws<ArgumentNullException>(() => new ConstraintPoint(null!));
        Assert.Throws<ArgumentNullException>(() => new ConstraintSpan(null!, new Node()));
        Assert.Throws<ArgumentNullException>(() => new ConstraintSpan(new Node(), null!));
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
        var span = new ConstraintSpan(a, c);

        List<Edge> result = span.Edges([]);

        Assert.Equal([ab, bc], result);
    }

    [Fact]
    public void Edges_ReturnsExistingListForZeroLengthReferenceSpan()
    {
        Node node = NodeAt(0, 0);
        var existing = new List<Edge>();

        List<Edge> result = new ConstraintSpan(node, node).Edges(existing);

        Assert.Same(existing, result);
        Assert.Empty(result);
    }

    [Fact]
    public void Edges_ThrowsWhenNoAlignedPathExists()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(0, 1);
        Node target = NodeAt(2, 0);
        a.Edge = DirectedEdge(a, b);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new ConstraintSpan(a, target).Edges([]));

        Assert.Contains("no aligned outgoing edge", exception.Message);
    }

    [Fact]
    public void Edges_ThrowsForDistinctCoincidentNodes()
    {
        Node a = NodeAt(1, 1);
        Node b = NodeAt(1, 1);

        Assert.Throws<InvalidOperationException>(() => new ConstraintSpan(a, b).Edges([]));
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

        Assert.Equal(expected, ConstraintSpan.NearlyColliniear(Vec2.UnitX, edge));
    }

    static Edge DirectedEdge(Node start, Node end)
    {
        var edge = new Edge { NodeStart = start };
        edge.Next = new Edge { NodeStart = end };
        return edge;
    }

    static Node NodeAt(double x, double y) => new() { Position = new Vec2(x, y) };
}
