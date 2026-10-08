using TriUgla;

namespace TriUgla.Tests;

public class PolylineTests
{
    [Fact]
    public void Polyline_RemainsOpenAndComputesLength()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(3, 4);
        Node c = NodeAt(3, 6);
        var polyline = new Polyline([a, b, c], "path");

        Assert.Equal(7, polyline.Length);
        Assert.Equal([a, b, c], polyline.Nodes);
        Assert.Equal("path", polyline.Name);
    }

    [Fact]
    public void Reverse_ReversesNodeOrderAndReturnsSamePolyline()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(1, 0);
        Node c = NodeAt(2, 0);
        var polyline = new Polyline([a, b, c]);

        Assert.Same(polyline, polyline.Reverse());

        Assert.Equal([c, b, a], polyline.Nodes);
    }

    [Fact]
    public void Edges_AppendsDirectedSegmentsWithoutClosingPath()
    {
        Node a = NodeAt(0, 0);
        Node b = NodeAt(1, 0);
        Node c = NodeAt(2, 0);
        Edge ab = DirectedEdge(a, b);
        Edge bc = DirectedEdge(b, c);
        a.Edge = ab;
        b.Edge = bc;
        var polyline = new Polyline([a, b, c]);

        List<Edge> result = polyline.Edges([]);

        Assert.Equal([ab, bc], result);
        Assert.Equal(3, polyline.Nodes.Count);
    }

    [Fact]
    public void Edges_UnitesConstraintSpanEdgesBetweenPolylineNodes()
    {
        Node a = NodeAt(0, 0);
        Node intermediate = NodeAt(1, 0);
        Node b = NodeAt(2, 0);
        Edge first = DirectedEdge(a, intermediate);
        Edge second = DirectedEdge(intermediate, b);
        a.Edge = first;
        intermediate.Edge = second;
        var polyline = new Polyline([a, b]);

        List<Edge> result = polyline.Edges([]);

        Assert.Equal([first, second], result);
    }

    static Edge DirectedEdge(Node start, Node end)
    {
        var edge = new Edge { NodeStart = start };
        edge.Next = new Edge { NodeStart = end };
        return edge;
    }

    static Node NodeAt(double x, double y) => new() { Position = new Vec2(x, y) };
}
