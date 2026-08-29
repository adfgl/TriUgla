namespace TriUgla.Tests;

public class NodeInserterTests
{
    [Fact]
    public void Insert_InsideFace_InterpolatesThreeNodesAndSplitsFace()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);

        InsertNodeResult result = inserter.Insert(new Vec2(0.5, 0.5));

        Assert.Equal(InsertNodeStatus.InsertedIntoFace, result.Status);
        Assert.NotNull(result.FaceSplit);
        Assert.Null(result.EdgeSplit);
        Assert.Equal(15, GetData(result.Node!).Number);
        Assert.Equal(InsertNodeStatus.InsertedIntoFace, GetData(result.Node!).InsertedStatus);
    }

    [Fact]
    public void Insert_OnEdge_InterpolatesTwoNodesAndSplitsEdge()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);

        InsertNodeResult result = inserter.Insert(new Vec2(1, 1));

        Assert.Equal(InsertNodeStatus.InsertedIntoEdge, result.Status);
        Assert.NotNull(result.EdgeSplit);
        Assert.Null(result.FaceSplit);
        Assert.Equal(30, GetData(result.Node!).Number);
        Assert.Equal(InsertNodeStatus.InsertedIntoEdge, GetData(result.Node!).InsertedStatus);
    }

    [Fact]
    public void Insert_OnExistingNode_UsesIncomingDataAndUpdatesNode()
    {
        Fixture fixture = CreateFixture();
        NodeInserter inserter = CreateInserter(fixture.Mesh);
        var incoming = new TestData("incoming", 99, null);

        InsertNodeResult result = inserter.Insert(fixture.A.Position, incoming);

        Assert.Equal(InsertNodeStatus.ExistingNodeDataUpdated, result.Status);
        Assert.Same(fixture.A, result.Node);
        Assert.NotSame(incoming, result.Node!.Data);
        Assert.Equal(99, GetData(result.Node).Number);
        Assert.Equal(InsertNodeStatus.ExistingNodeDataUpdated, GetData(result.Node).InsertedStatus);
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
        var interpolator = new TestInterpolator();
        return new NodeInserter(
            new NodeFactory(interpolator),
            new Splitter(interpolator),
            new MeshLocator(mesh));
    }

    static TestData GetData(Node node) => Assert.IsType<TestData>(node.Data);

    static Fixture CreateFixture()
    {
        var a = MakeNode(0, 0, 0);
        var b = MakeNode(2, 0, 20);
        var c = MakeNode(0, 2, 40);
        var d = MakeNode(2, 2, 60);

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

    static Node MakeNode(double x, double y, double value)
        => new()
        {
            Position = new Vec2(x, y),
            Data = new TestData("source", value, null)
        };

    sealed record Fixture(Mesh Mesh, Node A);
}
