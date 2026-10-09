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
        Assert.Equal(4, mesher.Mesh.Nodes().Count());
        Assert.Equal(3, mesher.Mesh.Faces().Count());

        RemoveNodeResult removal = mesher.Remove(inserted);

        Assert.True(removal.Removed);
        Assert.True(inserted.Dead);
        Assert.Equal(3, mesher.Mesh.Nodes().Count());
        Assert.Single(mesher.Mesh.Faces());
        Assert.IsType<Face>(mesher.Find(new Vec2(0.5, 0.5)));
    }

    [Fact]
    public void InsertLegalizesAffectedEdgesAfterEveryInsertion()
    {
        var mesher = new Mesher(new Vec2(-2, -2), new Vec2(4, 4), 4);
        Vec2[] positions =
        [
            new(0, 0), new(2, 0), new(0, 2), new(2, 2),
            new(0.4, 0.7), new(1.7, 1.2), new(1.1, 0.3)
        ];

        foreach (Vec2 position in positions)
        {
            Assert.Contains(
                mesher.Insert(position).Status,
                new[] { InsertNodeStatus.InsertedIntoFace, InsertNodeStatus.InsertedIntoEdge });
            AssertDelaunay(mesher);
        }
    }

    [Fact]
    public void RemoveByPositionRejectsPositionWithoutNode()
    {
        var mesher = new Mesher(CreateTriangle());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => mesher.Remove(new Vec2(0.5, 0.5)));

        Assert.Contains("no node exists", exception.Message);
    }

    [Fact]
    public void RemoveByPositionFindsMeshOwnedNode()
    {
        var mesher = new Mesher(CreateTriangle());
        Node live = mesher.Insert(new Vec2(0.5, 0.5)).Node!;

        RemoveNodeResult result = mesher.Remove(live.Position);

        Assert.True(result.Removed);
        Assert.True(live.Dead);
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
        Node[] nodes = mesher.Mesh.Nodes().ToArray();
        Node a = nodes.Single(node => node.Position == new Vec2(0, 0));
        Node b = nodes.Single(node => node.Position == new Vec2(2, 0));
        var point = new ConstraintPoint(a, "profile point");
        var span = new ConstraintLine(a, b, "profile line");

        Assert.True(mesher.TryInsertConstraint(span, out string? spanReason), spanReason);
        Assert.True(mesher.TryInsertConstraint(point, out string? pointReason), pointReason);
        Assert.Same(point, Assert.Single(mesher.Constraints.Points));
        Assert.Same(span, Assert.Single(mesher.Constraints.Lines));
        Assert.True(a.Constrained);
        Assert.True(Edge.Find(a, b)!.HasFeature);

        Assert.True(mesher.TryRemoveConstraint(
            new ConstraintLine(b, a), out string? removeSpanReason), removeSpanReason);
        Assert.True(mesher.TryRemoveConstraint(
            new ConstraintPoint(a), out string? removePointReason), removePointReason);
        Assert.Empty(mesher.Constraints.Points);
        Assert.Empty(mesher.Constraints.Lines);
        Assert.False(a.Constrained);
        Assert.False(Edge.Find(a, b)!.HasFeature);
    }

    [Fact]
    public void InsertAndRemovePolylineTracksFeatureEdges()
    {
        var mesher = new Mesher(CreateTriangle());
        Node[] nodes = mesher.Mesh.Nodes().ToArray();
        Node a = nodes.Single(node => node.Position == new Vec2(0, 0));
        Node b = nodes.Single(node => node.Position == new Vec2(2, 0));
        var polyline = new Polyline([a, b], "profile");

        Assert.True(mesher.TryInsertPolyline(polyline, out string? insertReason), insertReason);
        Assert.Same(polyline, Assert.Single(mesher.Constraints.Polylines));
        Assert.True(Edge.Find(a, b)!.HasFeature);

        Assert.True(mesher.TryRemovePolyline(
            new Polyline([b, a]), out string? removeReason), removeReason);
        Assert.Empty(mesher.Constraints.Polylines);
        Assert.False(Edge.Find(a, b)!.HasFeature);
    }

    [Fact]
    public void EverySegmentOfCrossingConstraintPathsIsMarkedFeatureConstrained()
    {
        var mesher = new Mesher(new Vec2(-1, -1), new Vec2(3, 3), 4);
        Node a = mesher.Insert(new Vec2(0, 0)).Node!;
        Node b = mesher.Insert(new Vec2(2, 2)).Node!;
        Node c = mesher.Insert(new Vec2(0, 2)).Node!;
        Node d = mesher.Insert(new Vec2(2, 0)).Node!;
        var first = new ConstraintLine(a, b);
        var second = new ConstraintLine(c, d);

        Assert.True(mesher.TryInsertConstraint(first, out string? firstReason), firstReason);
        Assert.True(mesher.TryInsertConstraint(second, out string? secondReason), secondReason);

        Assert.All(first.Edges([]), edge => Assert.True(edge.HasFeature));
        Assert.All(second.Edges([]), edge => Assert.True(edge.HasFeature));
        Assert.Equal(2, first.Edges([]).Count);
        Assert.Equal(2, second.Edges([]).Count);
    }

    [Fact]
    public void RemovingInsertedGeometryRemovesOnlyReleasedSteinerInsertions()
    {
        var mesher = new Mesher(new Vec2(-1, -1), new Vec2(3, 3), 4);
        Node a = mesher.Insert(new Vec2(0, 0)).Node!;
        Node b = mesher.Insert(new Vec2(2, 0)).Node!;
        Node c = mesher.Insert(new Vec2(2, 2)).Node!;
        Node d = mesher.Insert(new Vec2(0, 2)).Node!;
        var first = new ConstraintLine(a, c, "first diagonal");
        var second = new ConstraintLine(b, d, "second diagonal");
        Assert.True(mesher.TryInsertConstraint(first, out string? firstReason), firstReason);
        Assert.True(mesher.TryInsertConstraint(second, out string? secondReason), secondReason);
        Node intersection = Assert.Single(
            mesher.Mesh.Nodes(),
            node => node.Kind == NodeKind.SteinerInsertion);

        Assert.True(mesher.TryRemoveConstraint(second, out string? removeSecondReason), removeSecondReason);
        Assert.True(intersection.Dead);
        Edge[] restoredFirst = new ConstraintLine(a, c).Edges([]).ToArray();
        Assert.All(restoredFirst, edge => Assert.True(edge.HasFeature));
        Assert.DoesNotContain(restoredFirst, edge => edge.Contains(intersection));

        Assert.True(mesher.TryRemoveConstraint(first, out string? removeFirstReason), removeFirstReason);
        Assert.DoesNotContain(
            mesher.Mesh.Nodes(),
            node => node.Kind == NodeKind.SteinerInsertion);
    }

    [Theory]
    [InlineData(NodeKind.SteinerRefinement)]
    [InlineData(NodeKind.Normal)]
    public void ConstraintInsertionTemporarilyPromotesExistingNodeKind(NodeKind originalKind)
    {
        var mesher = new Mesher(new Vec2(-3, -2), new Vec2(3, 2), 4);
        Node start = mesher.Insert(new Vec2(-2, 0)).Node!;
        Node middle = mesher.Insert(Vec2.Zero).Node!;
        Node end = mesher.Insert(new Vec2(2, 0)).Node!;
        typeof(Node).GetProperty(nameof(Node.Kind))!
            .SetValue(middle, originalKind);
        var constraint = new ConstraintLine(start, end);

        Assert.True(mesher.TryInsertConstraint(constraint, out string? insertReason), insertReason);
        Assert.Equal(NodeKind.SteinerInsertion, middle.Kind);

        Assert.True(mesher.TryRemoveConstraint(constraint, out string? removeReason), removeReason);
        Assert.False(middle.Dead);
        Assert.Equal(originalKind, middle.Kind);
    }

    [Theory]
    [InlineData(NodeKind.SteinerInsertion)]
    [InlineData(NodeKind.SteinerRefinement)]
    public void RemovesNonStructuralSteinerFromConstrainedSpan(NodeKind kind)
    {
        var mesher = new Mesher(new Vec2(-3, -2), new Vec2(3, 2), 4);
        Node start = mesher.Insert(new Vec2(-2, 0)).Node!;
        Node middle = mesher.Insert(Vec2.Zero).Node!;
        Node end = mesher.Insert(new Vec2(2, 0)).Node!;
        var constraint = new ConstraintLine(start, end);
        Assert.True(mesher.TryInsertConstraint(constraint, out string? reason), reason);
        typeof(Node).GetProperty(nameof(Node.Kind))!.SetValue(middle, kind);

        RemoveNodeResult removal = mesher.Remove(middle);

        Assert.True(removal.Removed);
        Assert.True(middle.Dead);
        Edge[] path = constraint.Edges([]).ToArray();
        Assert.Single(path);
        Assert.True(path[0].HasFeature);
        Assert.Same(start, path[0].NodeStart);
        Assert.Same(end, path[0].NodeEnd);
    }

    [Fact]
    public void DoesNotRemoveStructuralSteinerAtConstraintIntersection()
    {
        var mesher = new Mesher(new Vec2(-1, -1), new Vec2(3, 3), 4);
        Node a = mesher.Insert(new Vec2(0, 0)).Node!;
        Node b = mesher.Insert(new Vec2(2, 2)).Node!;
        Node c = mesher.Insert(new Vec2(0, 2)).Node!;
        Node d = mesher.Insert(new Vec2(2, 0)).Node!;
        Assert.True(mesher.TryInsertConstraint(new ConstraintLine(a, b), out string? first), first);
        Assert.True(mesher.TryInsertConstraint(new ConstraintLine(c, d), out string? second), second);
        Node intersection = Assert.Single(mesher.Mesh.Nodes(), node =>
            node.Kind == NodeKind.SteinerInsertion);

        RemoveNodeResult removal = mesher.Remove(intersection);

        Assert.False(removal.Removed);
        Assert.False(intersection.Dead);
    }

    [Fact]
    public void InsertAndRemoveLoopTracksBoundaryEdges()
    {
        var mesher = new Mesher(CreateTriangle());
        Node[] nodes = mesher.Mesh.Nodes().ToArray();
        Node a = nodes.Single(node => node.Position == new Vec2(0, 0));
        Node b = nodes.Single(node => node.Position == new Vec2(2, 0));
        Node c = nodes.Single(node => node.Position == new Vec2(0, 2));
        var loop = new Loop([a, b, c], "domain");

        Assert.True(mesher.TryInsertLoop(loop, out string? insertReason), insertReason);
        Assert.Same(loop, Assert.Single(mesher.Constraints.Loops));
        Assert.All(loop.Edges([]), edge => Assert.True(edge.HasBoundary));

        Assert.True(mesher.TryRemoveLoop(
            new Loop([b, a, c]), out string? removeReason), removeReason);
        Assert.Empty(mesher.Constraints.Loops);
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
        Assert.Empty(mesher.Constraints.Loops);
        Assert.DoesNotContain(mesher.Mesh.Edges(), edge => edge.Constrained);
    }

    [Fact]
    public void SuperStructureConstructorRejectsItsNodesAsConstraints()
    {
        SuperStructure super = SuperStructure.Make(new Vec2(0, 0), new Vec2(2, 2));
        var mesher = new Mesher(super);
        Node node = super.Nodes.First();
        var constraint = new ConstraintPoint(node);

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
        FaceKind[] kinds = mesher.Mesh.Faces().Select(face => face.Kind).Distinct().ToArray();
        Assert.Contains(FaceKind.Outside, kinds);
        Assert.Contains(FaceKind.Island, kinds);
        Assert.DoesNotContain(FaceKind.Undefined, kinds);
    }

    [Fact]
    public void RepeatedRectangleRefinementDoesNotInsertMoreNodes()
    {
        var mesher = new Mesher(new Vec2(-1, -1), new Vec2(3, 2), 4);
        Node[] corners =
        [
            mesher.Insert(new Vec2(0, 0)).Node!,
            mesher.Insert(new Vec2(2, 0)).Node!,
            mesher.Insert(new Vec2(2, 1)).Node!,
            mesher.Insert(new Vec2(0, 1)).Node!
        ];
        foreach (Node node in corners)
        {
            node.Data = node.Data with { Area = 0.001 };
        }
        Assert.True(mesher.TryInsertLoop(new Loop(corners), out string? reason), reason);
        var ranker = new FaceRanker();
        RefineSettings settings = RefineSettings.Default;

        int first = mesher.Refine(ranker, settings);
        int second = mesher.Refine(ranker, settings);

        Assert.True(first > 1_000);
        Assert.Equal(0, second);
        Assert.DoesNotContain(
            mesher.Mesh.Faces(),
            face => face.Kind == FaceKind.Island && ranker.Rank(face) > 0);
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

    static void AssertDelaunay(Mesher mesher)
    {
        var flipper = new EdgeFlipper(mesher.Geometry);
        foreach (Edge edge in mesher.Mesh.Edges())
        {
            if (!flipper.CanFlip(edge, out bool shouldFlip)) continue;
            Assert.False(shouldFlip, $"Edge {edge.NodeStart.Position}–{edge.NodeEnd.Position} was not legalized.");
        }
    }
}
