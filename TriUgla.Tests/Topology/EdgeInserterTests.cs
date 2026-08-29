namespace TriUgla.Tests;

public class EdgeInserterTests
{
    [Fact]
    public void Insert_ExistingEdge_ConstrainsItWithoutChangingTopology()
    {
        Fixture fixture = CreateFixture();
        EdgeInserter inserter = CreateInserter();

        EdgeInsertResult result = inserter.Insert(fixture.A, fixture.B);

        Edge edge = Assert.Single(result.ConstrainedEdges);
        Assert.Same(fixture.A, edge.NodeStart);
        Assert.Same(fixture.B, edge.NodeEnd);
        Assert.Equal(1, edge.ConstraintCount);
        Assert.Empty(result.InsertedNodes);
        Assert.Empty(result.Change.AffectedFaces);
    }

    [Fact]
    public void Insert_AcrossFlippableEdge_FlipsAndConstrainsNewDiagonal()
    {
        Fixture fixture = CreateFixture();
        EdgeInserter inserter = CreateInserter();

        EdgeInsertResult result = inserter.Insert(fixture.A, fixture.D);

        Edge edge = Assert.Single(result.ConstrainedEdges);
        Assert.Same(fixture.A, edge.NodeStart);
        Assert.Same(fixture.D, edge.NodeEnd);
        Assert.Empty(result.InsertedNodes);
        Assert.Equal(2, result.Change.AffectedFaces.Count);
        Assert.Equal(2, result.Change.EdgesToLegalize.Count);
    }

    [Fact]
    public void Insert_WithSplitting_CreatesInterpolatedNodeAndConstrainedChain()
    {
        Fixture fixture = CreateFixture();
        EdgeInserter inserter = CreateInserter();
        inserter.SplitCrossedEdges = true;

        EdgeInsertResult result = inserter.Insert(fixture.A, fixture.D);

        Node inserted = Assert.Single(result.InsertedNodes);
        Assert.Equal(new Vec2(1, 1), inserted.Position);
        Assert.Equal(new NodeData(30, 40), inserted.Data);
        Assert.Equal(2, result.ConstrainedEdges.Count);
        Assert.All(result.ConstrainedEdges, edge => Assert.Equal(1, edge.ConstraintCount));
        Assert.Same(fixture.A, result.ConstrainedEdges[0].NodeStart);
        Assert.Same(inserted, result.ConstrainedEdges[0].NodeEnd);
        Assert.Same(inserted, result.ConstrainedEdges[1].NodeStart);
        Assert.Same(fixture.D, result.ConstrainedEdges[1].NodeEnd);
    }

    static EdgeInserter CreateInserter()
    {
        var geometry = new InsertionGeometry();
        return new EdgeInserter(
            geometry,
            new EdgeFlipper(geometry),
            new Splitter(),
            new NodeFactory());
    }

    static Fixture CreateFixture()
    {
        Node a = MakeNode(0, 0, 0, 10);
        Node b = MakeNode(2, 0, 20, 30);
        Node c = MakeNode(0, 2, 40, 50);
        Node d = MakeNode(2, 2, 60, 70);

        var ab = new Edge();
        var bc = new Edge();
        var ca = new Edge();
        var cb = new Edge();
        var bd = new Edge();
        var dc = new Edge();

        Linker.LinkTriangle(new Face(), ab, bc, ca, a, b, c);
        Linker.LinkTriangle(new Face(), cb, bd, dc, c, b, d);
        Linker.LinkTwins(bc, cb);

        return new Fixture(a, b, c, d);
    }

    static Node MakeNode(double x, double y, double z, double w)
        => new() { Position = new Vec2(x, y), Data = new NodeData(z, w) };

    sealed record Fixture(Node A, Node B, Node C, Node D);

    sealed class InsertionGeometry : IGeometry
    {
        public EOrientaiton Orient(Node a, Node b, Vec2 point)
            => Orient(b.Position - a.Position, point - a.Position);

        public EOrientaiton Orient(Edge edge, Vec2 point)
            => Orient(
                edge.NodeEnd.Position - edge.NodeStart.Position,
                point - edge.NodeStart.Position);

        public bool InDiameterCircle(Node a, Node b, Vec2 point) => false;

        public bool InCircumcircle(Node a, Node b, Node c, Vec2 point) => false;

        public bool IsConvexQuad(Quad quad) => true;

        static EOrientaiton Orient(Vec2 direction, Vec2 offset)
        {
            double cross = direction.Cross(offset);
            if (Math.Abs(cross) <= 1e-12)
            {
                return EOrientaiton.Collinear;
            }

            return cross > 0
                ? EOrientaiton.Counterclockwise
                : EOrientaiton.Clockwise;
        }
    }
}
