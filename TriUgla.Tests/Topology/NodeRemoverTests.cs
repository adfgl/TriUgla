namespace TriUgla.Tests;

public class NodeRemoverTests
{
    [Fact]
    public void Remove_RetriangulatesCavityAndPreservesBoundaryContext()
    {
        Fixture fixture = CreateFixture();
        fixture.AB.Constrain();

        RemoveNodeResult result = new NodeRemover().Remove(fixture.Center);

        Assert.True(result.Removed);
        Assert.True(fixture.Center.Dead);
        Assert.Equal(2, result.Change.AffectedFaces.Count);
        Assert.Equal(5, result.Change.EdgesToLegalize.Count);
        Assert.Equal(2, result.DeadFaces.Count);
        Assert.Equal(8, result.DeadEdges.Count);
        Assert.All(result.DeadFaces, face => Assert.True(face.Dead));
        Assert.All(result.DeadEdges, edge => Assert.True(edge.Dead));
        Assert.All(result.Change.AffectedFaces, AssertTriangle);

        Assert.False(fixture.AB.Dead);
        Assert.Equal(1, fixture.AB.ConstraintCount);
        Assert.Contains(result.Change.AffectedFaces, face => ReferenceEquals(face, fixture.AB.Face));
    }

    [Fact]
    public void Remove_LinksNewInternalDiagonalAsTwins()
    {
        Fixture fixture = CreateFixture();

        RemoveNodeResult result = new NodeRemover().Remove(fixture.Center);

        Edge[] boundary = [fixture.AB, fixture.BC, fixture.CD, fixture.DA];
        Edge[] internalEdges = result.Change.AffectedFaces
            .SelectMany(face => face.Edges)
            .Where(edge => !boundary.Contains(edge))
            .ToArray();

        Assert.Equal(2, internalEdges.Length);
        Assert.Same(internalEdges[1], internalEdges[0].Twin);
        Assert.Same(internalEdges[0], internalEdges[1].Twin);
        Assert.Same(internalEdges[0].NodeStart, internalEdges[1].NodeEnd);
        Assert.Same(internalEdges[0].NodeEnd, internalEdges[1].NodeStart);
    }

    [Fact]
    public void Remove_ConstrainedNode_ReturnsFailureWithoutMutation()
    {
        Fixture fixture = CreateFixture();
        fixture.Center.Constrain();

        RemoveNodeResult result = new NodeRemover().Remove(fixture.Center);

        Assert.False(result.Removed);
        Assert.False(fixture.Center.Dead);
        Assert.Empty(result.Change.AffectedFaces);
        Assert.Empty(result.Change.EdgesToLegalize);
        Assert.All(fixture.Faces, face => Assert.False(face.Dead));
    }

    static void AssertTriangle(Face face)
    {
        Edge[] edges = face.Edges.ToArray();
        Assert.Equal(3, edges.Length);
        Assert.All(edges, edge => Assert.Same(face, edge.Face));
        Assert.Same(edges[0], edges[2].Next);
        Assert.False(face.Dead);
    }

    static Fixture CreateFixture()
    {
        var center = new Node { Position = Vec2.Zero };
        var a = new Node { Position = new Vec2(-1, -1) };
        var b = new Node { Position = new Vec2(1, -1) };
        var c = new Node { Position = new Vec2(1, 1) };
        var d = new Node { Position = new Vec2(-1, 1) };

        var oa = new Edge();
        var ab = new Edge();
        var bo = new Edge();
        var ob = new Edge();
        var bc = new Edge();
        var co = new Edge();
        var oc = new Edge();
        var cd = new Edge();
        var @do = new Edge();
        var od = new Edge();
        var da = new Edge();
        var ao = new Edge();
        Face[] faces = [new(), new(), new(), new()];

        Linker.LinkTriangle(faces[0], od, da, ao, center, d, a);
        Linker.LinkTriangle(faces[1], oa, ab, bo, center, a, b);
        Linker.LinkTriangle(faces[2], ob, bc, co, center, b, c);
        Linker.LinkTriangle(faces[3], oc, cd, @do, center, c, d);
        Linker.LinkTwins(oa, ao);
        Linker.LinkTwins(ob, bo);
        Linker.LinkTwins(oc, co);
        Linker.LinkTwins(od, @do);

        return new Fixture(center, ab, bc, cd, da, faces);
    }

    sealed record Fixture(
        Node Center,
        Edge AB,
        Edge BC,
        Edge CD,
        Edge DA,
        Face[] Faces);
}
