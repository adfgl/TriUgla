namespace TriUgla.Tests;

public class FaceClassifierTests
{
    [Fact]
    public void ClassifiesClockwiseHoleAndCounterClockwiseInnerSolid()
    {
        var mesher = new Mesher(new Vec2(-6, -6), new Vec2(6, 6), 4);
        InsertLoop(mesher,
            [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)],
            "outer solid");
        InsertLoop(mesher,
            [new(-4, -4), new(4, -4), new(4, 4), new(-4, 4)],
            "nested solid");
        InsertLoop(mesher,
            [new(-3, -3), new(-3, 3), new(3, 3), new(3, -3)],
            "hole");
        InsertLoop(mesher,
            [new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)],
            "inner solid");

        new FaceClassifier(mesher.Mesh, mesher.SuperStructure!).Classify();

        Assert.Equal(FaceKind.Outside, FaceAt(mesher, new Vec2(5.5, 0)).Kind);
        Assert.Equal(FaceKind.Island, FaceAt(mesher, new Vec2(4.5, 0)).Kind);
        Assert.Equal(FaceKind.Island, FaceAt(mesher, new Vec2(3.5, 0)).Kind);
        Assert.Equal(FaceKind.Lake, FaceAt(mesher, new Vec2(2, 0)).Kind);
        Assert.Equal(FaceKind.Island, FaceAt(mesher, new Vec2(0, 0)).Kind);
    }

    [Fact]
    public void ClassifiesCounterClockwiseSolidAndClockwiseHoleByBoundaryDirection()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Twin!.Constrain(EdgeConstraintKind.Boundary);
        chain.IslandToLake.Constrain(EdgeConstraintKind.Boundary);

        Face result = new FaceClassifier(
            chain.Mesh,
            chain.SuperStructure).Classify();

        Assert.Same(chain.Root, result);
        Assert.Equal(FaceKind.Outside, chain.Outside.Kind);
        Assert.Equal(FaceKind.Island, chain.Island.Kind);
        Assert.Equal(FaceKind.Lake, chain.Lake.Kind);
    }

    [Fact]
    public void FeatureConstraintDoesNotChangeFaceKind()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Constrain(EdgeConstraintKind.Feature);

        new FaceClassifier(chain.Mesh, chain.SuperStructure).Classify();

        Assert.All(
            new[] { chain.Outside, chain.Island, chain.Lake },
            face => Assert.Equal(FaceKind.Outside, face.Kind));
    }

    [Fact]
    public void BoundaryOnTwinIsRecognized()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Twin!.Constrain(EdgeConstraintKind.Boundary);

        new FaceClassifier(chain.Mesh, chain.SuperStructure).Classify();

        Assert.Equal(FaceKind.Outside, chain.Outside.Kind);
        Assert.Equal(FaceKind.Island, chain.Island.Kind);
        Assert.Equal(FaceKind.Island, chain.Lake.Kind);
    }

    [Fact]
    public void SplittingClassifiedFacePreservesKind()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Twin!.Constrain(EdgeConstraintKind.Boundary);
        new FaceClassifier(chain.Mesh, chain.SuperStructure).Classify();

        FaceSplitResult split = new Splitter().Split(chain.Island, new Node());

        Assert.All(split.Change.AffectedFaces, face => Assert.Equal(FaceKind.Island, face.Kind));
    }

    [Fact]
    public void ThrowsWhenMeshHasNoSuperNodeFace()
    {
        SuperStructure structure = SuperStructure.Make(new Vec2(-10, -10), new Vec2(10, 10));
        Face face = Triangle(new Node(), new Node(), new Node());

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                var mesh = new Mesh(face);
                new FaceClassifier(
                    mesh,
                    structure).Classify();
            });
    }

    static Chain CreateChain()
    {
        SuperStructure structure = SuperStructure.Make(new Vec2(-10, -10), new Vec2(10, 10));
        Node super = structure.Nodes.First();
        Node a = new();
        Node b = new();
        Node c = new();
        Node d = new();

        Face outside = Triangle(super, a, b);
        Face island = Triangle(b, a, c);
        Face lake = Triangle(c, a, d);
        Edge outsideToIsland = outside.Edge.Next;
        Edge islandToLake = island.Edge.Next;
        Linker.LinkTwins(outsideToIsland, island.Edge);
        Linker.LinkTwins(islandToLake, lake.Edge);

        var mesh = new Mesh(outside);
        return new Chain(
            outside,
            mesh,
            structure,
            outside,
            island,
            lake,
            outsideToIsland,
            islandToLake);
    }

    static Face Triangle(Node a, Node b, Node c)
    {
        var face = new Face();
        Linker.LinkTriangle(face, new Edge(), new Edge(), new Edge(), a, b, c);
        return face;
    }

    static void InsertLoop(Mesher mesher, IReadOnlyList<Vec2> positions, string name)
    {
        Node[] nodes = positions.Select(position => mesher.Insert(position).Node!).ToArray();
        Assert.True(mesher.TryInsertLoop(new Loop(nodes, name), out string? reason), reason);
    }

    static Face FaceAt(Mesher mesher, Vec2 position)
    {
        LocateResult location = mesher.Locate(position);
        return Assert.IsType<Face>(
            location.Face ?? location.Edge?.Face ?? location.Node?.Edge?.Face);
    }

    sealed record Chain(
        Face Root,
        Mesh Mesh,
        SuperStructure SuperStructure,
        Face Outside,
        Face Island,
        Face Lake,
        Edge OutsideToIsland,
        Edge IslandToLake);
}
