using TriUgla;

namespace TriUgla.Tests;

public class LoopTests
{
    [Fact]
    public void Close_AppendsFirstNodeByReferenceAndIsIdempotent()
    {
        Node a = NodeAt(0, 0);
        var loop = new Loop([a, NodeAt(1, 0), NodeAt(0, 1)], "triangle");

        Assert.Same(loop, loop.Close());
        loop.Close();

        Assert.True(loop.Closed());
        Assert.Equal(4, loop.Nodes.Count);
        Assert.Same(a, loop.Nodes[^1]);
        Assert.Equal("triangle", loop.Name);
    }

    [Fact]
    public void EmptyLoop_IsSafeToCloseAndHasZeroArea()
    {
        var loop = new Loop([]);

        loop.Close();

        Assert.False(loop.Closed());
        Assert.Equal(0, loop.SignedArea());
    }

    [Fact]
    public void SignedArea_IsPositiveCounterClockwiseAndNegativeClockwise()
    {
        var counterClockwise = new Loop([
            NodeAt(0, 0), NodeAt(2, 0), NodeAt(0, 1)]);
        var clockwise = new Loop(counterClockwise.Nodes.AsEnumerable().Reverse());

        Assert.Equal(1, counterClockwise.SignedArea());
        Assert.Equal(-1, clockwise.SignedArea());
    }

    [Fact]
    public void ForceClockwise_ReversesInteriorOrderButPreservesFirstAndClosure()
    {
        Node first = NodeAt(0, 0);
        Node second = NodeAt(2, 0);
        Node third = NodeAt(0, 1);
        var loop = new Loop([first, second, third]);

        Assert.Same(loop, loop.ForceClockwise());

        Assert.True(loop.SignedArea() < 0);
        Assert.Equal([first, third, second, first], loop.Nodes);
    }

    [Fact]
    public void ForceCounterClockwise_ReversesClockwiseLoop()
    {
        Node first = NodeAt(0, 0);
        var loop = new Loop([first, NodeAt(0, 1), NodeAt(2, 0)]);

        Assert.Same(loop, loop.ForceCounterClockwise());

        Assert.True(loop.SignedArea() > 0);
        Assert.Same(first, loop.Nodes[0]);
        Assert.Same(first, loop.Nodes[^1]);
    }

    [Fact]
    public void Edges_ClosesLoopAndAppendsDirectedEdges()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(1, 0);
        Node c = NodeAt(0, 1);
        Edge ab = DirectedEdge(a, b);
        Edge bc = DirectedEdge(b, c);
        Edge ca = DirectedEdge(c, a);
        a.Edge = ab;
        b.Edge = bc;
        c.Edge = ca;
        var loop = new Loop([a, b, c]);

        List<Edge> result = loop.Edges([]);

        Assert.Equal([ab, bc, ca], result);
    }

    [Fact]
    public void Edges_MissingDirectedSegmentReportsItsIndex()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(1, 0);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new Loop([a, b]).Edges([]));

        Assert.Contains("loop segment 0", exception.Message);
    }

    static Edge DirectedEdge(Node start, Node end)
    {
        var edge = new Edge { NodeStart = start };
        edge.Next = new Edge { NodeStart = end };
        return edge;
    }

    static Node NodeAt(double x, double y) => new() { Position = new Vec2(x, y) };
}
