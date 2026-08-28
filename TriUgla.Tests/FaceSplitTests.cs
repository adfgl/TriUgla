namespace TriUgla.Tests;

public class FaceSplitTests
{
    [Fact]
    public void SplitPreservesLinksAndOriginalTwins()
    {
        var (target, boundary, originalTwins) = CreateFace(3, withTwins: true);

        FaceSplitResult result = new Splitter().Split(target, new Node());

        for (int i = 0; i < result.AffectedFaces.Count; i++)
        {
            AssertTriangleRing(result.AffectedFaces[i], boundary[i]);
            Assert.Same(originalTwins[i], boundary[i].Twin);
            Assert.Same(boundary[i], originalTwins[i].Twin);
        }

        Assert.Equal(3, result.AffectedFaces.Count);
        Assert.Same(target, result.AffectedFaces[0]);
        Assert.Equal(boundary, result.EdgesToLegalize);
    }

    [Fact]
    public void SplitRejectsNonTriangularFace()
    {
        var (face, _, _) = CreateFace(4, withTwins: false);

        var error = Assert.Throws<InvalidOperationException>(
            () => new Splitter().Split(face, new Node()));

        Assert.Contains("requires triangular faces", error.Message);
    }

    [Fact]
    public void Split_CopiesFaceDataToNewFaces()
    {
        var (target, _, _) = CreateFace(3, withTwins: false);
        target.Data = new TestData("green", 0, null);

        FaceSplitResult result = new Splitter(new TestInterpolator())
            .Split(target, new Node());

        Assert.All(result.AffectedFaces, face =>
            Assert.Equal("green", Assert.IsType<TestData>(face.Data).Color));
    }

    static void AssertTriangleRing(Face face, Edge boundary)
    {
        Edge first = face.Edge;
        Edge second = first.Next;
        Edge third = second.Next;

        Assert.Same(boundary, first);
        Assert.Same(first, third.Next);
        Assert.Same(third, first.Prev);
        Assert.Same(first, second.Prev);
        Assert.Same(second, third.Prev);

        Assert.Same(face, first.Face);
        Assert.Same(face, second.Face);
        Assert.Same(face, third.Face);

        Assert.Same(second, second.Twin!.Twin);
        Assert.Same(third, third.Twin!.Twin);
    }

    static (Face Face, Edge[] Boundary, Edge[] Twins) CreateFace(
        int edgeCount,
        bool withTwins)
    {
        var face = new Face();
        var nodes = Enumerable.Range(0, edgeCount).Select(_ => new Node()).ToArray();
        var edges = nodes.Select(node => new Edge
        {
            NodeStart = node,
            Face = face
        }).ToArray();
        var twins = new Edge[edgeCount];

        for (int i = 0; i < edgeCount; i++)
        {
            edges[i].Next = edges[(i + 1) % edgeCount];
            edges[i].Prev = edges[(i - 1 + edgeCount) % edgeCount];
            nodes[i].Edge = edges[i];

            if (withTwins)
            {
                twins[i] = new Edge();
                Linker.LinkTwins(edges[i], twins[i]);
            }
        }

        face.Edge = edges[0];
        return (face, edges, twins);
    }
}
