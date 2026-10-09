namespace TriUgla.Tests;

public class MeshDegraderTests
{
    static readonly Vec2 CandidatePosition = new(0.2, 0.1);

    [Fact]
    public void KeepsRemovalWhenReplacementFacesAreCloserToLargeTargetArea()
    {
        Mesher mesher = CreateMesher(out Node candidate);
        SetTargetArea(mesher, 2000d);

        int removed = mesher.Degrade([candidate]);

        Assert.Equal(1, removed);
        Assert.True(candidate.Dead);
    }

    [Fact]
    public void RestoresNodeWhenRemovalIncreasesTargetAreaError()
    {
        Mesher mesher = CreateMesher(out Node candidate);
        SetTargetArea(mesher, 0.001d);
        candidate.Data = new NodeData(7d, 0.001d);

        int removed = mesher.Degrade([candidate]);

        Assert.Equal(0, removed);
        Node restored = Assert.Single(mesher.Mesh.Nodes(), node =>
            !node.Dead && node.Position == CandidatePosition);
        Assert.Equal(NodeKind.SteinerRefinement, restored.Kind);
        Assert.Equal(new NodeData(7d, 0.001d), restored.Data);
    }

    [Fact]
    public void DoesNotDegradeWithoutAUsableTargetArea()
    {
        Mesher mesher = CreateMesher(out Node candidate);

        int removed = mesher.Degrade([candidate]);

        Assert.Equal(0, removed);
        Assert.False(candidate.Dead);
    }

    [Fact]
    public void CanDegradeNonStructuralSteinerOnConstrainedSpan()
    {
        var mesher = new Mesher(new Vec2(-3, -2), new Vec2(3, 2), 4);
        Node start = mesher.Insert(new Vec2(-2, 0)).Node!;
        Node middle = mesher.Insert(Vec2.Zero).Node!;
        Node end = mesher.Insert(new Vec2(2, 0)).Node!;
        var constraint = new ConstraintLine(start, end);
        Assert.True(mesher.TryInsertConstraint(constraint, out string? reason), reason);
        SetTargetArea(mesher, 2000d);

        int removed = mesher.Degrade([middle]);

        Assert.Equal(1, removed);
        Assert.True(middle.Dead);
        Assert.Single(constraint.Edges([]));
        Assert.All(constraint.Edges([]), edge => Assert.True(edge.HasFeature));
    }

    static Mesher CreateMesher(out Node candidate)
    {
        var mesher = new Mesher(new Vec2(-10, -10), new Vec2(10, 10), 4);
        InsertNodeResult insertion = mesher.Insert(CandidatePosition);
        candidate = Assert.IsType<Node>(insertion.Node);
        typeof(Node).GetProperty(nameof(Node.Kind))!.SetValue(
            candidate, NodeKind.SteinerRefinement);
        return mesher;
    }

    static void SetTargetArea(Mesher mesher, double area)
    {
        foreach (Node node in mesher.Mesh.Nodes())
            node.Data = node.Data with { Area = area };
    }
}
