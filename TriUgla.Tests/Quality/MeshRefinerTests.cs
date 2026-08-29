namespace TriUgla.Tests;

public class MeshRefinerTests
{
    [Fact]
    public void RefineInsertsCircumcenterIntoBadFace()
    {
        Fixture fixture = CreateFixture();
        FaceRanker ranker = AreaRanker(1);

        int inserted = fixture.Refiner.Refine(
            [fixture.Face],
            ranker,
            new RefineSettings(1, 8, 1e-4));

        Assert.Equal(1, inserted);
        Assert.Equal(3, fixture.Traversal.Faces().Count());
        Node[] nodes = fixture.Traversal.Nodes().ToArray();
        Assert.Equal(4, nodes.Length);
        Assert.Contains(nodes, node => node.Position.Distance(new Vec2(1, 0.75)) < 1e-12);
    }

    [Fact]
    public void RefineHonorsSteinerBudget()
    {
        Fixture fixture = CreateFixture();

        int inserted = fixture.Refiner.Refine(
            [fixture.Face],
            AreaRanker(1),
            new RefineSettings(0, 8, 1e-4));

        Assert.Equal(0, inserted);
        Assert.Single(fixture.Traversal.Faces());
        Assert.Equal(3, fixture.Traversal.Nodes().Count());
    }

    [Fact]
    public void EncroachedDetectsNodeInsideDiameterCircle()
    {
        Fixture fixture = CreateFixture(new Vec2(1, 0.1));
        Edge edge = fixture.Face.Edge;

        Assert.True(fixture.Refiner.Encroached(edge));
        Assert.False(fixture.Refiner.Encroached(edge, new Vec2(1, 2)));
    }

    [Fact]
    public void RefineSplitsEncroachedConstrainedSegmentBeforeFaces()
    {
        Node a = new() { Position = new Vec2(0.5, 0.5) };
        Node b = new() { Position = new Vec2(2, 0) };
        Node c = new() { Position = new Vec2(0, 2) };
        Node d = new() { Position = new Vec2(3, 3) };
        Edge ab = new();
        Edge bc = new();
        Edge ca = new();
        Edge cb = new();
        Edge bd = new();
        Edge dc = new();
        Face first = new();
        Face second = new();
        Linker.LinkTriangle(first, ab, bc, ca, a, b, c);
        Linker.LinkTriangle(second, cb, bd, dc, c, b, d);
        Linker.LinkTwins(bc, cb);
        bc.Constrain(EdgeConstraintKind.Boundary);

        Fixture fixture = CreateFixture(first);
        FaceRanker ranker = AreaRanker(double.PositiveInfinity);
        int inserted = fixture.Refiner.Refine(
            [first, second], ranker, new RefineSettings(1, 8, 1e-4));

        Assert.Equal(1, inserted);
        Assert.Equal(5, fixture.Traversal.Nodes().Count());
        Node midpoint = Assert.Single(
            fixture.Traversal.Nodes(),
            node => node.Position == new Vec2(1, 1));
        Assert.True(midpoint.Constrained);
        Assert.Equal(1, bc.ConstraintCount);
    }

    [Fact]
    public void RefineSplitsEncroachedBoundarySegment()
    {
        Fixture fixture = CreateFixture(new Vec2(1, 0.1));
        fixture.Face.Edge.Constrain(EdgeConstraintKind.Boundary);

        int inserted = fixture.Refiner.Refine(
            [fixture.Face],
            AreaRanker(double.PositiveInfinity),
            new RefineSettings(1, 8, 1e-4));

        Assert.Equal(1, inserted);
        Assert.Equal(2, fixture.Traversal.Faces().Count());
        Assert.Equal(4, fixture.Traversal.Nodes().Count());
        Assert.Contains(
            fixture.Traversal.Nodes(),
            node => node.Position == new Vec2(1, 0) && node.Constrained);
    }

    [Fact]
    public void RefineRejectsInvalidSettings()
    {
        Fixture fixture = CreateFixture();

        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Refiner.Refine(
            [fixture.Face],
            AreaRanker(1),
            new RefineSettings(-1, 0, 0)));
    }

    [Fact]
    public void RefineObservesCancellationBeforeProcessingWork()
    {
        Fixture fixture = CreateFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => fixture.Refiner.Refine(
            [fixture.Face],
            AreaRanker(1),
            new RefineSettings(10, 8, 1e-4),
            cancellation.Token));
        Assert.Single(fixture.Traversal.Faces());
    }

    [Fact]
    public async Task RefineAsyncObservesCancellationBeforeProcessingWork()
    {
        Fixture fixture = CreateFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await fixture.Refiner.RefineAsync(
                [fixture.Face],
                AreaRanker(1),
                new RefineSettings(10, 8, 1e-4),
                cancellation.Token));
        Assert.Single(fixture.Traversal.Faces());
    }

    static FaceRanker AreaRanker(double maxArea)
    {
        var ranker = new FaceRanker();
        ranker.Angle.Weight = 0;
        ranker.VertexArea.Weight = 0;
        ranker.Area.Weight = 1;
        ranker.Area.MaxArea = maxArea;
        return ranker;
    }

    static Fixture CreateFixture(Vec2? third = null)
    {
        Node a = new() { Position = new Vec2(0, 0) };
        Node b = new() { Position = new Vec2(2, 0) };
        Node c = new() { Position = third ?? new Vec2(1, 2) };
        Face face = new();
        Linker.LinkTriangle(face, new Edge(), new Edge(), new Edge(), a, b, c);

        return CreateFixture(face);
    }

    static Fixture CreateFixture(Face face)
    {
        var stamps = new StampSource();
        var traversal = new MeshTraversal(face, stamps);
        var locator = new MeshLocator(face, traversal, stamps);
        var geometry = new GeometryPredicates();
        var splitter = new Splitter();
        var legalizer = new EdgeLegalizer(new EdgeFlipper(geometry));
        var inserter = new NodeInserter(new NodeFactory(), splitter, locator);
        var refiner = new MeshRefiner(
            geometry,
            locator,
            legalizer,
            splitter,
            inserter,
            traversal);
        return new Fixture(face, traversal, refiner);
    }

    sealed record Fixture(Face Face, MeshTraversal Traversal, MeshRefiner Refiner);
}
