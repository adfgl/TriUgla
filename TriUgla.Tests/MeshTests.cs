namespace TriUgla.Tests;

public class MeshTests
{
    [Fact]
    public void TraversesEachElementOnce()
    {
        AssertTraversesEachElementOnce();
    }

    static void AssertTraversesEachElementOnce()
    {
        Face root = CreateTwoTriangles();
        var mesh = new Mesh(root);

        Assert.Same(root, mesh.Root);
        Assert.Equal(2, mesh.Faces().Count());
        Assert.Equal(6, mesh.Edges().Count());
        Assert.Equal(4, mesh.Nodes().Count());

        MeshSnapshot snapshot = mesh.Snapshot();
        Assert.Equal(2, snapshot.Faces.Count);
        Assert.Equal(6, snapshot.Edges.Count);
        Assert.Equal(4, snapshot.Nodes.Count);
    }

    [Fact]
    public void CanTraverseAcrossEdgeLimitsTraversal()
    {
        Face root = CreateTwoTriangles();
        var mesh = new Mesh(root);

        Face[] faces = mesh
            .Faces(canTraverse: (_, _, _) => false)
            .ToArray();

        Assert.Single(faces);
        Assert.Same(root, faces[0]);
    }

    [Fact]
    public void ResetVisitStampsResetsAllReachableElements()
    {
        var mesh = new Mesh(CreateTwoTriangles());
        MeshSnapshot snapshot = mesh.Snapshot();
        var stamp = new Stamp(42);

        MarkVisited(snapshot.Faces, stamp);
        MarkVisited(snapshot.Edges, stamp);
        MarkVisited(snapshot.Nodes, stamp);

        mesh.ResetVisitStamps();

        AssertReset(snapshot.Faces, stamp);
        AssertReset(snapshot.Edges, stamp);
        AssertReset(snapshot.Nodes, stamp);
    }

    static void MarkVisited<T>(IEnumerable<T> elements, Stamp stamp)
        where T : MeshElement
    {
        foreach (T element in elements)
        {
            Assert.True(element.TryVisit(stamp));
            Assert.False(element.TryVisit(stamp));
        }
    }

    static void AssertReset<T>(IEnumerable<T> elements, Stamp stamp)
        where T : MeshElement
    {
        foreach (T element in elements)
        {
            Assert.True(element.TryVisit(stamp));
        }
    }

    static Face CreateTwoTriangles()
    {
        var a = new Node();
        var b = new Node();
        var c = new Node();
        var d = new Node();

        var ab = new Edge();
        var bc = new Edge();
        var ca = new Edge();
        var ba = new Edge();
        var ad = new Edge();
        var db = new Edge();
        var top = new Face();
        var bottom = new Face();

        Linker.LinkTwins(ab, ba);
        Linker.LinkTriangle(top, ab, bc, ca, a, b, c);
        Linker.LinkTriangle(bottom, ba, ad, db, b, a, d);

        return top;
    }
}
