namespace TriUgla.Tests;

public class MesherTests
{
    [Fact]
    public void InsertAndRemoveNode()
    {
        var mesher = new Mesher(CreateTriangle());

        InsertNodeResult insertion = mesher.Insert(new Vec2(0.5, 0.5));

        Assert.Equal(InsertNodeStatus.InsertedIntoFace, insertion.Status);
        Node inserted = Assert.IsType<Node>(insertion.Node);
        Assert.Equal(4, mesher.Traversal.Nodes().Count());
        Assert.Equal(3, mesher.Traversal.Faces().Count());

        RemoveNodeResult removal = mesher.Remove(inserted);

        Assert.True(removal.Removed);
        Assert.True(inserted.Dead);
        Assert.Equal(3, mesher.Traversal.Nodes().Count());
        Assert.Single(mesher.Traversal.Faces());
        Assert.IsType<Face>(mesher.Find(new Vec2(0.5, 0.5)));
    }

    [Fact]
    public void RemoveRejectsNodeFromAnotherMesh()
    {
        var mesher = new Mesher(CreateTriangle());
        var foreign = new Node { Position = new Vec2(0.5, 0.5) };

        RemoveNodeResult result = mesher.Remove(foreign);

        Assert.False(result.Removed);
        Assert.False(foreign.Dead);
    }

    [Fact]
    public void ExposesRootFace()
    {
        Face root = CreateTriangle();

        Assert.Same(root, new Mesher(root).Root);
    }

    [Fact]
    public void InsertAndRemoveConstraintTracksFeaturesAndPoints()
    {
        var mesher = new Mesher(CreateTriangle());
        Node[] nodes = mesher.Traversal.Nodes().ToArray();
        Node a = nodes.Single(node => node.Position == new Vec2(0, 0));
        Node b = nodes.Single(node => node.Position == new Vec2(2, 0));
        var constraint = new Constraint(
            [new ConstraintPoint(a)],
            [new ConstraintSpan(a, b)],
            "profile");

        Assert.True(mesher.TryInsertConstraint(constraint, out string? insertReason), insertReason);
        Assert.Same(constraint, Assert.Single(mesher.Constraints));
        Assert.True(a.Constrained);
        Assert.True(Edge.Find(a, b)!.HasFeature);

        Assert.True(mesher.TryRemoveConstraint(constraint, out string? removeReason), removeReason);
        Assert.Empty(mesher.Constraints);
        Assert.False(a.Constrained);
        Assert.False(Edge.Find(a, b)!.HasFeature);
    }

    [Fact]
    public void InsertAndRemoveLoopTracksBoundaryEdges()
    {
        var mesher = new Mesher(CreateTriangle());
        Node[] nodes = mesher.Traversal.Nodes().ToArray();
        Node a = nodes.Single(node => node.Position == new Vec2(0, 0));
        Node b = nodes.Single(node => node.Position == new Vec2(2, 0));
        Node c = nodes.Single(node => node.Position == new Vec2(0, 2));
        var loop = new Loop([a, b, c], "domain");

        Assert.True(mesher.TryInsertLoop(loop, out string? insertReason), insertReason);
        Assert.Same(loop, Assert.Single(mesher.Loops));
        Assert.All(loop.Edges([]), edge => Assert.True(edge.HasBoundary));

        Assert.True(mesher.TryRemoveLoop(loop, out string? removeReason), removeReason);
        Assert.Empty(mesher.Loops);
        Assert.All(loop.Edges([]), edge => Assert.False(edge.HasBoundary));
    }

    [Fact]
    public void LoopValidationRejectsSelfIntersectionWithoutChangingMesh()
    {
        var mesher = new Mesher(new Vec2(-1, -1), new Vec2(3, 3), 4);
        Node a = mesher.Insert(new Vec2(0, 0)).Node!;
        Node b = mesher.Insert(new Vec2(2, 2)).Node!;
        Node c = mesher.Insert(new Vec2(0, 2)).Node!;
        Node d = mesher.Insert(new Vec2(1.5, -0.5)).Node!;
        var loop = new Loop([a, b, c, d], "bow-tie");

        Assert.False(mesher.TryInsertLoop(loop, out string? reason));
        Assert.Contains("self-intersecting", reason);
        Assert.Empty(mesher.Loops);
        Assert.DoesNotContain(mesher.Traversal.Edges(), edge => edge.Constrained);
    }

    [Fact]
    public void SuperStructureConstructorRejectsItsNodesAsConstraints()
    {
        SuperStructure super = SuperStructure.Make(new Vec2(0, 0), new Vec2(2, 2));
        var mesher = new Mesher(super);
        Node node = super.Nodes.First();
        var constraint = new Constraint([new ConstraintPoint(node)]);

        Assert.False(mesher.TryInsertConstraint(constraint, out string? reason));
        Assert.Contains("super structure", reason);
    }

    [Fact]
    public void RefineClassifiesFacesBeforeApplyingSteinerBudget()
    {
        var mesher = new Mesher(new Vec2(-1, -1), new Vec2(3, 3), 4);
        Node a = mesher.Insert(new Vec2(0, 0)).Node!;
        Node b = mesher.Insert(new Vec2(2, 0)).Node!;
        Node c = mesher.Insert(new Vec2(0, 2)).Node!;
        Assert.True(mesher.TryInsertLoop(new Loop([a, b, c]), out string? reason), reason);

        int inserted = mesher.Refine(
            new FaceRanker(),
            new RefineSettings(0, 8, 1e-4));

        Assert.Equal(0, inserted);
        FaceKind[] kinds = mesher.Traversal.Faces().Select(face => face.Kind).Distinct().ToArray();
        Assert.Contains(FaceKind.Outside, kinds);
        Assert.Contains(FaceKind.Island, kinds);
        Assert.DoesNotContain(FaceKind.Undefined, kinds);
    }

    [Fact]
    public void RefineRequiresClassificationContext()
    {
        var mesher = new Mesher(CreateTriangle());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            mesher.Refine(new FaceRanker(), new RefineSettings(0, 8, 1e-4)));

        Assert.Contains("classified", exception.Message);
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
