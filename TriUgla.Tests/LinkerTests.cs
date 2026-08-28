namespace TriUgla.Tests;

public class LinkerTests
{
    [Fact]
    public void LinkTwinsLinksBothDirections()
    {
        var first = new Edge();
        var second = new Edge();

        Linker.LinkTwins(first, second);

        Assert.Same(second, first.Twin);
        Assert.Same(first, second.Twin);
    }

    [Fact]
    public void LinkNodeEdgeLinksBothDirections()
    {
        var node = new Node();
        var edge = new Edge();

        Linker.LinkNodeEdge(node, edge);

        Assert.Same(node, edge.NodeStart);
        Assert.Same(edge, node.Edge);
    }

    [Fact]
    public void LinkEdgesLinksNextAndPrevious()
    {
        var first = new Edge();
        var next = new Edge();

        Linker.LinkEdges(first, next);

        Assert.Same(next, first.Next);
        Assert.Same(first, next.Prev);
    }

    [Fact]
    public void LinkFaceEdgesAssignsFaceToAllEdges()
    {
        var face = new Face();
        var first = new Edge();
        var second = new Edge();
        var third = new Edge();

        Linker.LinkFaceEdges(face, first, second, third);

        Assert.Same(first, face.Edge);
        Assert.Same(face, first.Face);
        Assert.Same(face, second.Face);
        Assert.Same(face, third.Face);
    }

    [Fact]
    public void LinkTriangleCreatesClosedTriangle()
    {
        var face = new Face();
        var edges = new[] { new Edge(), new Edge(), new Edge() };
        var nodes = new[] { new Node(), new Node(), new Node() };

        Linker.LinkTriangle(
            face,
            edges[0], edges[1], edges[2],
            nodes[0], nodes[1], nodes[2]);

        Assert.Same(edges[0], face.Edge);
        for (int i = 0; i < 3; i++)
        {
            int next = (i + 1) % 3;
            int previous = (i + 2) % 3;

            Assert.Same(nodes[i], edges[i].NodeStart);
            Assert.Same(edges[i], nodes[i].Edge);
            Assert.Same(edges[next], edges[i].Next);
            Assert.Same(edges[previous], edges[i].Prev);
            Assert.Same(face, edges[i].Face);
        }
    }
}
