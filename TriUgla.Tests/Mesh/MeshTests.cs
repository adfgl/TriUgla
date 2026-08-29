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
        MeshTraversal traversal = mesh.Traversal;

        Assert.Same(root, mesh.Root);
        Assert.Equal(2, traversal.Faces().Count());
        Assert.Equal(6, traversal.Edges().Count());
        Assert.Equal(4, traversal.Nodes().Count());

        MeshSnapshot snapshot = traversal.Snapshot();
        Assert.Equal(2, snapshot.Faces.Count);
        Assert.Equal(6, snapshot.Edges.Count);
        Assert.Equal(4, snapshot.Nodes.Count);
    }

    [Fact]
    public void CanTraverseAcrossEdgeLimitsTraversal()
    {
        Face root = CreateTwoTriangles();
        var mesh = new Mesh(root);

        Face[] faces = mesh.Traversal
            .Faces(canTraverse: (_, _, _) => false)
            .ToArray();

        Assert.Single(faces);
        Assert.Same(root, faces[0]);
    }

    [Fact]
    public void LocateUsesMeshLocatorAndReturnsTypedResult()
    {
        var mesh = new Mesh(CreateTwoTriangles());

        LocateResult result = mesh.Locate(new Vec2(0.25, 0.25));

        Assert.True(result.IsFace);
        Assert.Same(mesh.Root, result.Face);
    }

    [Fact]
    public void FindReturnsMostSpecificElementAtPoint()
    {
        var mesh = new Mesh(CreateTwoTriangles());
        Edge shared = mesh.Root.Edge.Next;

        Assert.Same(shared.NodeStart, mesh.Find(shared.NodeStart.Position));
        Assert.Same(shared, mesh.Find(new Vec2(1, 1)));
        Assert.Same(mesh.Root, mesh.Find(new Vec2(0.25, 0.25)));
        Assert.Null(mesh.Find(new Vec2(3, 3)));
    }

    [Fact]
    public void ResetVisitStampsResetsAllReachableElements()
    {
        var mesh = new Mesh(CreateTwoTriangles());
        MeshSnapshot snapshot = mesh.Traversal.Snapshot();
        var stamp = new Stamp(42);

        MarkVisited(snapshot.Faces, stamp);
        MarkVisited(snapshot.Edges, stamp);
        MarkVisited(snapshot.Nodes, stamp);

        mesh.Traversal.ResetVisitStamps();

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
        var a = new Node { Position = new Vec2(0, 0) };
        var b = new Node { Position = new Vec2(2, 0) };
        var c = new Node { Position = new Vec2(0, 2) };
        var d = new Node { Position = new Vec2(2, 2) };

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
