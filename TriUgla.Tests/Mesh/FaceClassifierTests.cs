namespace TriUgla.Tests;

public class FaceClassifierTests
{
    [Fact]
    public void ClassifiesOutsideIslandAndLakeByBoundaryDepth()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Constrain(EdgeConstraintKind.Boundary);
        chain.IslandToLake.Constrain(EdgeConstraintKind.Boundary);

        Face result = new FaceClassifier(
            chain.Root,
            chain.Traversal,
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

        new FaceClassifier(chain.Root, chain.Traversal, chain.SuperStructure).Classify();

        Assert.All(
            new[] { chain.Outside, chain.Island, chain.Lake },
            face => Assert.Equal(FaceKind.Outside, face.Kind));
    }

    [Fact]
    public void BoundaryOnTwinIsRecognized()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Twin!.Constrain(EdgeConstraintKind.Boundary);

        new FaceClassifier(chain.Root, chain.Traversal, chain.SuperStructure).Classify();

        Assert.Equal(FaceKind.Outside, chain.Outside.Kind);
        Assert.Equal(FaceKind.Island, chain.Island.Kind);
        Assert.Equal(FaceKind.Island, chain.Lake.Kind);
    }

    [Fact]
    public void SplittingClassifiedFacePreservesKind()
    {
        Chain chain = CreateChain();
        chain.OutsideToIsland.Constrain(EdgeConstraintKind.Boundary);
        new FaceClassifier(chain.Root, chain.Traversal, chain.SuperStructure).Classify();

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
                var stamps = new StampSource();
                new FaceClassifier(
                    face,
                    new MeshTraversal(face, stamps),
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

        var stamps = new StampSource();
        return new Chain(
            outside,
            new MeshTraversal(outside, stamps),
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

    sealed record Chain(
        Face Root,
        MeshTraversal Traversal,
        SuperStructure SuperStructure,
        Face Outside,
        Face Island,
        Face Lake,
        Edge OutsideToIsland,
        Edge IslandToLake);
}
