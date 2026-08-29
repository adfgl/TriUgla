namespace TriUgla.Tests;

public class NodeInserterTests
{
    [Fact]
    public void Insert_InsideFace_InterpolatesZWAndSplitsFace()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);

        InsertNodeResult result = inserter.Insert(new Vec2(0.5, 0.5));

        Assert.Equal(InsertNodeStatus.InsertedIntoFace, result.Status);
        Assert.NotNull(result.FaceSplit);
        Assert.Null(result.EdgeSplit);
        Assert.Equal(new NodeData(15, 25), result.Node!.Data);
    }

    [Fact]
    public void Insert_OnEdge_InterpolatesZWAndSplitsEdge()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);

        InsertNodeResult result = inserter.Insert(new Vec2(1, 1));

        Assert.Equal(InsertNodeStatus.InsertedIntoEdge, result.Status);
        Assert.NotNull(result.EdgeSplit);
        Assert.Null(result.FaceSplit);
        Assert.Equal(new NodeData(30, 40), result.Node!.Data);
    }

    [Fact]
    public void Insert_OnExistingNode_PreservesStoredZW()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);
        InsertNodeResult result = inserter.Insert(fixture.A.Position);

        Assert.Equal(InsertNodeStatus.ExistingNodeDataUpdated, result.Status);
        Assert.Same(fixture.A, result.Node);
        Assert.Equal(new NodeData(0, 10), result.Node!.Data);
    }

    [Fact]
    public void Insert_OutsideMesh_ReturnsOutsideWithoutNode()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);

        InsertNodeResult result = inserter.Insert(new Vec2(3, 3));

        Assert.Equal(InsertNodeStatus.Outside, result.Status);
        Assert.Null(result.Node);
        Assert.True(result.Location.IsEmpty);
    }

    static NodeInserter CreateInserter(Mesh mesh)
    {
        return new NodeInserter(
            new NodeFactory(),
            new Splitter(),
            new MeshLocator(mesh));
    }

    static Fixture CreateFixture()
    {
        var a = MakeNode(0, 0, 0, 10);
        var b = MakeNode(2, 0, 20, 30);
        var c = MakeNode(0, 2, 40, 50);
        var d = MakeNode(2, 2, 60, 70);

        var ab = new Edge();
        var bc = new Edge();
        var ca = new Edge();
        var cb = new Edge();
        var bd = new Edge();
        var dc = new Edge();
        var first = new Face();
        var second = new Face();

        Linker.LinkTriangle(first, ab, bc, ca, a, b, c);
        Linker.LinkTriangle(second, cb, bd, dc, c, b, d);
        Linker.LinkTwins(bc, cb);

        return new Fixture(new Mesh(first), a);
    }

    static Node MakeNode(double x, double y, double z, double w)
        => new() { Position = new Vec2(x, y), Data = new NodeData(z, w) };

    sealed record Fixture(Mesh Mesh, Node A);
}
