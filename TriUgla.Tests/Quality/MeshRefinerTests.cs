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
        Assert.Equal(3, fixture.Mesh.Faces().Count());
        Node[] nodes = fixture.Mesh.Nodes().ToArray();
        Assert.Equal(4, nodes.Length);
        Node steiner = Assert.Single(
            nodes,
            node => node.Position.Distance(new Vec2(1, 0.75)) < 1e-12);
        Assert.Equal(NodeKind.SteinerRefinement, steiner.Kind);
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
        Assert.Single(fixture.Mesh.Faces());
        Assert.Equal(3, fixture.Mesh.Nodes().Count());
    }

    [Fact]
    public void DetailedResultReportsBudgetExhaustionAsIncomplete()
    {
        Fixture fixture = CreateFixture();

        RefineResult result = fixture.Refiner.RefineDetailed(
            [fixture.Face],
            AreaRanker(1),
            new RefineSettings(0, 8, 1e-4));

        Assert.Equal(RefineStatus.SteinerBudgetReached, result.Status);
        Assert.False(result.Completed);
        Assert.Equal(0, result.InsertedNodes);
        Assert.Equal(1, result.RemainingBadFaces);
    }

    [Fact]
    public void RefineAlwaysAllowsFirstFaceAttemptWhenStagnationBudgetIsZero()
    {
        Fixture fixture = CreateFixture();

        int inserted = fixture.Refiner.Refine(
            [fixture.Face],
            AreaRanker(1),
            new RefineSettings(1, 0, 0));

        Assert.Equal(1, inserted);
        Assert.Equal(4, fixture.Mesh.Nodes().Count());
    }

    [Fact]
    public void RefineConsidersLandButNotLakesByDefault()
    {
        Fixture land = CreateFixture();
        SetKind(land.Face, FaceKind.Island);
        Fixture lake = CreateFixture();
        SetKind(lake.Face, FaceKind.Lake);

        int landInserted = land.Refiner.Refine(
            [land.Face], AreaRanker(1), RefineSettings.Default);
        int lakeInserted = lake.Refiner.Refine(
            [lake.Face], AreaRanker(1), RefineSettings.Default);

        Assert.Equal(1, landInserted);
        Assert.Equal(0, lakeInserted);
    }

    [Fact]
    public void RefineCanTargetLakesWithoutConsideringLand()
    {
        Fixture lake = CreateFixture();
        SetKind(lake.Face, FaceKind.Lake);
        RefineSettings settings = RefineSettings.Default with
        {
            RefineLand = false,
            RefineLakes = true
        };

        int inserted = lake.Refiner.Refine([lake.Face], AreaRanker(1), settings);

        Assert.Equal(1, inserted);
    }

    [Fact]
    public void RefineConvergesInOneRunForNodeTargetAreas()
    {
        Fixture fixture = CreateFixture(new Vec2(1, Math.Sqrt(3)));
        foreach (Edge edge in fixture.Face.Edges)
        {
            edge.Constrain(EdgeConstraintKind.Boundary);
        }
        foreach (Node node in fixture.Mesh.Nodes())
        {
            node.Data = node.Data with { Area = 0.05 };
        }
        var ranker = new FaceRanker();
        ranker.Angle.Weight = 0;
        var settings = new RefineSettings(10_000, 8, 1e-4);

        int first = fixture.Refiner.Refine([fixture.Face], ranker, settings);
        int second = fixture.Refiner.Refine(
            fixture.Mesh.Faces().ToArray(), ranker, settings);

        Assert.True(first > 0);
        Assert.Equal(0, second);
        Assert.All(fixture.Mesh.Faces(), face => Assert.Equal(0, ranker.Rank(face)));
    }

    [Fact]
    public void DetailedResultConfirmsCompletedMeshIsIdempotent()
    {
        Fixture fixture = CreateFixture(new Vec2(1, Math.Sqrt(3)));
        foreach (Edge edge in fixture.Face.Edges)
            edge.Constrain(EdgeConstraintKind.Boundary);
        foreach (Node node in fixture.Mesh.Nodes())
            node.Data = node.Data with { Area = 0.05 };
        var ranker = new FaceRanker();
        ranker.Angle.Weight = 0;

        RefineResult first = fixture.Refiner.RefineDetailed(
            [fixture.Face], ranker, RefineSettings.Default);
        RefineResult second = fixture.Refiner.RefineDetailed(
            fixture.Mesh.Faces().ToArray(), ranker, RefineSettings.Default);

        Assert.Equal(RefineStatus.Completed, first.Status);
        Assert.Equal(0, first.RemainingBadFaces);
        Assert.Equal(0, first.RemainingEncroachedSegments);
        Assert.Equal(RefineStatus.Completed, second.Status);
        Assert.Equal(0, second.InsertedNodes);
    }

    [Fact]
    public void DetailedResultDoesNotClaimCompletionForUnchangedFailedGeometry()
    {
        Fixture fixture = CreateFixture();

        RefineResult result = fixture.Refiner.RefineDetailed(
            [fixture.Face], AreaRanker(0.1), RefineSettings.Default);

        Assert.Equal(RefineStatus.NumericalStagnation, result.Status);
        Assert.False(result.Completed);
        Assert.True(result.RemainingBadFaces > 0);
        Assert.NotNull(result.FailureReason);
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
        Assert.Equal(5, fixture.Mesh.Nodes().Count());
        Node midpoint = Assert.Single(
            fixture.Mesh.Nodes(),
            node => node.Position == new Vec2(1, 1));
        Assert.True(midpoint.Constrained);
        Assert.Equal(NodeKind.SteinerRefinement, midpoint.Kind);
        Assert.Equal(1, bc.ConstraintCount);
    }

    [Fact]
    public void CircumcenterThatEncroachesSegmentIsRejectedAndSegmentIsSplitFirst()
    {
        Node a = new() { Position = new Vec2(0, 2) };
        Node b = new() { Position = new Vec2(-1, 0) };
        Node c = new() { Position = new Vec2(1, 0) };
        Node d = new() { Position = new Vec2(0, -2) };
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

        int inserted = fixture.Refiner.Refine(
            [first, second],
            AreaRanker(1),
            new RefineSettings(1, 0, 0));

        Assert.Equal(1, inserted);
        Assert.Contains(
            fixture.Mesh.Nodes(),
            node => node.Position == Vec2.Zero && node.Constrained);
        Assert.DoesNotContain(
            fixture.Mesh.Nodes(),
            node => node.Position == new Vec2(0, 0.75));
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
        Assert.Equal(2, fixture.Mesh.Faces().Count());
        Assert.Equal(4, fixture.Mesh.Nodes().Count());
        Assert.Contains(
            fixture.Mesh.Nodes(),
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
        Assert.Single(fixture.Mesh.Faces());
    }

    [Fact]
    public void RefineObservesCancellationWhenBudgetPreventsLoopEntry()
    {
        Fixture fixture = CreateFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => fixture.Refiner.Refine(
            [fixture.Face],
            AreaRanker(1),
            new RefineSettings(0, 8, 1e-4),
            cancellation.Token));
        Assert.Single(fixture.Mesh.Faces());
    }

    [Fact]
    public void RefineRejectsReentrantInvocation()
    {
        Fixture fixture = CreateFixture();

        IEnumerable<Face> ReentrantFaces()
        {
            fixture.Refiner.Refine(
                [fixture.Face],
                AreaRanker(1),
                new RefineSettings(0, 8, 1e-4));
            yield return fixture.Face;
        }

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Refiner.Refine(
                ReentrantFaces(),
                AreaRanker(1),
                new RefineSettings(0, 8, 1e-4)));

        Assert.Contains("already running", error.Message);
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

    static void SetKind(Face face, FaceKind kind)
        => typeof(Face).GetProperty(nameof(Face.Kind))!.SetValue(face, kind);

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
        var mesh = new Mesh(face);
        var locator = new MeshLocator(mesh);
        var geometry = new GeometryPredicates();
        var splitter = new Splitter();
        var legalizer = new EdgeLegalizer(new EdgeFlipper(geometry));
        var inserter = new NodeInserter(new NodeFactory(), splitter, locator);
        var refiner = new MeshRefiner(
            geometry,
            locator,
            legalizer,
            splitter,
            inserter);
        return new Fixture(face, mesh, refiner);
    }

    sealed record Fixture(Face Face, Mesh Mesh, MeshRefiner Refiner);
}
