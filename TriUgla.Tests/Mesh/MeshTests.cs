namespace TriUgla.Tests;

public class MeshTests
{
    [Fact]
    public void RootGetterRecoversFromDeadFace()
    {
        (RemoveNodeResult removal, Face deadFace) = RemoveInsertedCenter();
        Linker.LinkTwins(deadFace.Edge, removal.Change.AffectedFaces[0].Edge);
        var mesh = new Mesh(deadFace);

        Face recovered = mesh.Root;

        Assert.False(recovered.Dead);
        Assert.Contains(recovered, removal.Change.AffectedFaces);
        Assert.Same(recovered, mesh.Root);
    }

    [Fact]
    public void RootGetterThrowsWhenNoLiveFaceIsReachable()
    {
        (_, Face deadFace) = RemoveInsertedCenter();
        foreach (Edge edge in deadFace.Edges)
        {
            edge.Twin = null;
            edge.Face = deadFace;
            edge.NodeStart.Edge = edge;
        }
        var mesh = new Mesh(deadFace);

        Assert.Throws<InvalidOperationException>(() => _ = mesh.Root);
    }

    [Fact]
    public void MeshExposesOnlyRootProperty()
        => Assert.Equal(
            new[] { nameof(Mesh.Root) },
            typeof(Mesh).GetProperties().Select(property => property.Name));

    [Fact]
    public void TraversesEachElementOnce()
    {
        AssertTraversesEachElementOnce();
    }

    static void AssertTraversesEachElementOnce()
    {
        Face root = CreateTwoTriangles();
        var mesher = new Mesher(root);
        MeshTraversal traversal = mesher.Traversal;

        Assert.Same(root, mesher.Root);
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
        var mesher = new Mesher(root);

        Face[] faces = mesher.Traversal
            .Faces(canTraverse: (_, _, _) => false)
            .ToArray();

        Assert.Single(faces);
        Assert.Same(root, faces[0]);
    }

    [Fact]
    public void LocateUsesMeshLocatorAndReturnsTypedResult()
    {
        var mesher = new Mesher(CreateTwoTriangles());

        LocateResult result = mesher.Locate(new Vec2(0.25, 0.25));

        Assert.True(result.IsFace);
        Assert.Same(mesher.Root, result.Face);
    }

    [Fact]
    public void FindReturnsMostSpecificElementAtPoint()
    {
        var mesher = new Mesher(CreateTwoTriangles());
        Edge shared = mesher.Root.Edge.Next;

        Assert.Same(shared.NodeStart, mesher.Find(shared.NodeStart.Position));
        Assert.Same(shared, mesher.Find(new Vec2(1, 1)));
        Assert.Same(mesher.Root, mesher.Find(new Vec2(0.25, 0.25)));
        Assert.Null(mesher.Find(new Vec2(3, 3)));
    }

    [Fact]
    public void ResetVisitStampsResetsAllReachableElements()
    {
        var mesher = new Mesher(CreateTwoTriangles());
        MeshSnapshot snapshot = mesher.Traversal.Snapshot();
        var stamp = new Stamp(42);

        MarkVisited(snapshot.Faces, stamp);
        MarkVisited(snapshot.Edges, stamp);
        MarkVisited(snapshot.Nodes, stamp);

        mesher.Traversal.ResetVisitStamps();

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

    static (RemoveNodeResult Removal, Face DeadFace) RemoveInsertedCenter()
    {
        var mesher = new Mesher(CreateTriangle());
        Node center = mesher.Insert(new Vec2(0.5, 0.5)).Node!;
        RemoveNodeResult removal = new NodeRemover().Remove(center);
        return (removal, removal.DeadFaces.First());
    }

    static Face CreateTriangle()
    {
        var face = new Face();
        Linker.LinkTriangle(
            face,
            new Edge(), new Edge(), new Edge(),
            new Node { Position = new Vec2(0, 0) },
            new Node { Position = new Vec2(2, 0) },
            new Node { Position = new Vec2(0, 2) });
        return face;
    }

}
