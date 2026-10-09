using TriUgla.MeshEditor;

namespace TriUgla.Tests;

public class EditorPolylineTests
{
    [Fact]
    public void NodePropertiesCanBeEditedUndoneAndRedone()
    {
        var editor = new EditorMeshModel();
        int id = Insert(editor, 0.25, 0.5);

        MeshView updated = editor.UpdateNodeData(id, 12.5, 0.75);

        Assert.True(updated.Succeeded, updated.FailureReason);
        NodeView node = Assert.Single(updated.Nodes, candidate => candidate.Id == id);
        Assert.Equal(12.5, node.Elevation);
        Assert.Equal(0.75, node.TargetArea);

        NodeView undone = Assert.Single(editor.Undo().Nodes, candidate => candidate.Id == id);
        Assert.Equal(default, undone.Elevation);
        Assert.Equal(default, undone.TargetArea);

        NodeView redone = Assert.Single(editor.Redo().Nodes, candidate => candidate.Id == id);
        Assert.Equal(12.5, redone.Elevation);
        Assert.Equal(0.75, redone.TargetArea);
    }

    [Fact]
    public void NodePropertiesRejectInvalidValues()
    {
        var editor = new EditorMeshModel();
        int id = Insert(editor, 0.25, 0.5);

        MeshView negativeArea = editor.UpdateNodeData(id, 1, -0.1);
        MeshView nonFiniteElevation = editor.UpdateNodeData(id, double.NaN, 1);

        Assert.False(negativeArea.Succeeded);
        Assert.False(nonFiniteElevation.Succeeded);
        NodeView node = Assert.Single(editor.State().Nodes, candidate => candidate.Id == id);
        Assert.Equal(default, node.Elevation);
        Assert.Equal(default, node.TargetArea);
    }

    [Fact]
    public void PropertyEditAppliesToMultipleNodesAsOneUndoableAction()
    {
        var editor = new EditorMeshModel();
        int first = Insert(editor, -1, 0);
        int second = Insert(editor, 1, 0);
        Assert.True(editor.UpdateNodeData(first, 10, 2).Succeeded);
        Assert.True(editor.UpdateNodeData(second, 20, 4).Succeeded);

        MeshView updated = editor.UpdateNodeProperty(
            [first, second], "Target area", 0.75);

        Assert.True(updated.Succeeded, updated.FailureReason);
        Assert.Equal([10d, 20d], updated.Nodes
            .Where(node => node.Id == first || node.Id == second)
            .OrderBy(node => node.Id)
            .Select(node => node.Elevation).ToArray());
        Assert.All(updated.Nodes.Where(node => node.Id == first || node.Id == second),
            node => Assert.Equal(0.75, node.TargetArea));

        MeshView undone = editor.Undo();
        Assert.Equal([2d, 4d], undone.Nodes
            .Where(node => node.Id == first || node.Id == second)
            .OrderBy(node => node.Id)
            .Select(node => node.TargetArea).ToArray());

        MeshView redone = editor.Redo();
        Assert.All(redone.Nodes.Where(node => node.Id == first || node.Id == second),
            node => Assert.Equal(0.75, node.TargetArea));
    }

    [Fact]
    public void SelectedFaceCanBeRefinedUsingNodeTargetAreas()
    {
        var editor = new EditorMeshModel();
        foreach (int nodeId in new[] { 4, 5, 6, 7 })
            Assert.True(editor.UpdateNodeData(nodeId, 0, 0.5).Succeeded);
        MeshView before = editor.State();
        int faceId = before.Faces.Select((face, index) => (face, index))
            .First(item => item.face.Kind == nameof(FaceKind.Island)).index;

        MeshView refined = editor.RefineFaces([faceId]);

        Assert.True(refined.Succeeded, refined.FailureReason);
        Assert.True(refined.Nodes.Count > before.Nodes.Count);
        Assert.Contains(refined.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerRefinement));
    }

    [Fact]
    public void RefinementRequiresAValidFaceSelection()
    {
        var editor = new EditorMeshModel();

        Assert.False(editor.RefineFaces([]).Succeeded);
        Assert.False(editor.RefineFaces([int.MaxValue]).Succeeded);
    }

    [Fact]
    public void RefineAllRefinesWithoutAFaceSelection()
    {
        var editor = new EditorMeshModel();
        foreach (int nodeId in new[] { 4, 5, 6, 7 })
            Assert.True(editor.UpdateNodeData(nodeId, 0, 0.5).Succeeded);
        int before = editor.State().Nodes.Count;

        MeshView refined = editor.RefineAll();

        Assert.True(refined.Succeeded, refined.FailureReason);
        Assert.True(refined.Nodes.Count > before);
    }

    [Fact]
    public void DensityBrushEstablishesAndScalesLocalTargetArea()
    {
        var editor = new EditorMeshModel();

        MeshView first = editor.BrushDensity([0d, 0d], 10d, increaseDensity: false);
        MeshView second = editor.BrushDensity([0d, 0d], 10d, increaseDensity: false);

        Assert.True(first.Succeeded, first.FailureReason);
        Assert.True(second.Succeeded, second.FailureReason);
        NodeView firstCorner = Assert.Single(first.Nodes, node => node.Id == 4);
        NodeView secondCorner = Assert.Single(second.Nodes, node => node.Id == 4);
        Assert.True(firstCorner.TargetArea > 0d);
        Assert.Equal(firstCorner.TargetArea * 1.01d, secondCorner.TargetArea, 10);
    }

    [Fact]
    public void DecreaseDensityBrushCanCoarsenARefinementSteiner()
    {
        var editor = new EditorMeshModel();
        MeshView inserted = editor.Insert(0, 0);
        int id = Assert.IsType<int>(inserted.ChangedNodeId);
        var ids = Assert.IsType<Dictionary<Node, int>>(
            typeof(EditorMeshModel)
                .GetField("_ids", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .GetValue(editor));
        Node node = ids.Single(pair => pair.Value == id).Key;
        typeof(Node).GetProperty(nameof(Node.Kind))!.SetValue(node, NodeKind.SteinerRefinement);
        foreach (NodeView editable in editor.State().Nodes.Where(candidate => !candidate.IsSuper))
            Assert.True(editor.UpdateNodeData(editable.Id, editable.Elevation, 2000d).Succeeded);

        MeshView result = editor.BrushDensity([0d, 0d], 10d, increaseDensity: false);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.DoesNotContain(result.Nodes, candidate => candidate.X == 0d && candidate.Y == 0d);
    }

    [Fact]
    public void DensityBrushRejectsInvalidStroke()
    {
        var editor = new EditorMeshModel();

        MeshView result = editor.BrushDensity([0d], 1d, increaseDensity: true);

        Assert.False(result.Succeeded);
        Assert.Contains("finite points", result.FailureReason);
    }

    [Theory]
    [InlineData("percentage", 10d, true, 9d)]
    [InlineData("percentage", 10d, false, 11d)]
    [InlineData("absolute", 2d, true, 8d)]
    [InlineData("absolute", 2d, false, 12d)]
    [InlineData("value", 3d, true, 3d)]
    [InlineData("value", 3d, false, 3d)]
    public void DensityBrushSupportsConfiguredEffects(
        string mode,
        double effect,
        bool increaseDensity,
        double expected)
    {
        var editor = new EditorMeshModel();
        foreach (int id in new[] { 4, 5, 6, 7 })
            Assert.True(editor.UpdateNodeData(id, 0d, 10d).Succeeded);

        MeshView result = editor.BrushDensity(
            [0d, 0d], 10d, increaseDensity, effectMode: mode, effectValue: effect);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.All(result.Nodes.Where(node => !node.IsSuper),
            node => Assert.Equal(expected, node.TargetArea, 10));
    }

    [Theory]
    [InlineData("percentage", 10d, true, 11d)]
    [InlineData("percentage", 10d, false, 9d)]
    [InlineData("absolute", 2d, true, 12d)]
    [InlineData("absolute", 2d, false, 8d)]
    [InlineData("value", 3d, true, 3d)]
    [InlineData("value", 3d, false, 3d)]
    [InlineData("value", -3d, true, -3d)]
    public void ElevationBrushSupportsConfiguredEffects(
        string mode,
        double effect,
        bool increase,
        double expected)
    {
        var editor = new EditorMeshModel();
        foreach (int id in new[] { 4, 5, 6, 7 })
            Assert.True(editor.UpdateNodeData(id, 10d, 0d).Succeeded);
        int nodeCount = editor.State().Nodes.Count;

        MeshView result = editor.BrushDensity(
            [0d, 0d], 10d, increase, effectMode: mode,
            effectValue: effect, propertyName: "Elevation");

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.Equal(nodeCount, result.Nodes.Count);
        Assert.All(result.Nodes.Where(node => !node.IsSuper),
            node => Assert.Equal(expected, node.Elevation, 10));
    }

    [Fact]
    public void InsertedPolygonCanBeRemovedAndRestoredThroughHistory()
    {
        var editor = new EditorMeshModel();
        int a = Insert(editor, -2, -1);
        int b = Insert(editor, 2, -1);
        int c = Insert(editor, 0, 2);
        int initialBoundaryCount = editor.State().LoopEdges.Count;

        MeshView inserted = editor.InsertPolygon([a, b, c]);

        Assert.True(inserted.Succeeded, inserted.FailureReason);
        Assert.True(inserted.LoopEdges.Count > initialBoundaryCount);

        MeshView removed = editor.Undo();
        Assert.True(removed.Succeeded, removed.FailureReason);
        Assert.Equal(initialBoundaryCount, removed.LoopEdges.Count);

        MeshView restored = editor.Redo();
        Assert.True(restored.Succeeded, restored.FailureReason);
        Assert.Equal(inserted.LoopEdges.Count, restored.LoopEdges.Count);
    }

    [Fact]
    public void InsertedPolylineCanBeRemovedAndUndone()
    {
        var editor = new EditorMeshModel();
        int first = Insert(editor, -3, 2);
        int middle = Insert(editor, 0, -2);
        int last = Insert(editor, 3, 2);
        MeshView inserted = editor.InsertPolyline([first, middle, last]);
        Assert.True(inserted.Succeeded);
        Assert.Single(inserted.Constraints);

        MeshView removed = editor.RemoveConstraint(0);

        Assert.True(removed.Succeeded);
        Assert.Empty(removed.Constraints);
        MeshView restored = editor.Undo();
        Assert.True(restored.Succeeded);
        Assert.Single(restored.Constraints);
    }

    [Fact]
    public void GroupRemovalCreatesOneUndoEntry()
    {
        var editor = new EditorMeshModel();
        int a = Insert(editor, -3, 2);
        int b = Insert(editor, -1, -2);
        int c = Insert(editor, 1, -2);
        int d = Insert(editor, 3, 2);
        Assert.True(editor.InsertConstraint(a, b).Succeeded);
        Assert.True(editor.InsertPolyline([c, d]).Succeeded);

        MeshView removed = editor.RemoveElements([0, 1], []);

        Assert.True(removed.Succeeded);
        Assert.Empty(removed.Constraints);
        MeshView restored = editor.Undo();
        Assert.True(restored.Succeeded);
        Assert.Equal(2, restored.Constraints.Count);
        MeshView previous = editor.Undo();
        Assert.Single(previous.Constraints);
    }

    [Fact]
    public void GroupRemovalAllowsSteinerAlreadyRemovedWithConstraint()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int bottom = Insert(editor, 0, -2);
        int top = Insert(editor, 0, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        MeshView crossed = editor.InsertConstraint(bottom, top);
        Assert.True(crossed.Succeeded);
        int intersection = Assert.Single(
            crossed.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion)).Id;

        MeshView removed = editor.RemoveElements([0], [intersection]);

        Assert.True(removed.Succeeded);
        Assert.Single(removed.Constraints);
        Assert.DoesNotContain(removed.Nodes, node => node.Id == intersection);

        MeshView survivorRemoved = editor.RemoveElements([0], []);
        Assert.True(survivorRemoved.Succeeded);
        Assert.Empty(survivorRemoved.Constraints);
        Assert.True(editor.Undo().Succeeded);

        MeshView restored = editor.Undo();
        Assert.True(restored.Succeeded);
        Assert.Equal(2, restored.Constraints.Count);
        Assert.Contains(restored.Nodes, node => node.Kind == nameof(NodeKind.SteinerInsertion));

        MeshView redone = editor.Redo();
        Assert.True(redone.Succeeded);
        Assert.Single(redone.Constraints);
        Assert.DoesNotContain(redone.Nodes, node => node.Kind == nameof(NodeKind.SteinerInsertion));

        MeshView restoredAgain = editor.Undo();
        Assert.True(restoredAgain.Succeeded);
        Assert.Equal(2, restoredAgain.Constraints.Count);
        Assert.Contains(restoredAgain.Nodes, node => node.Kind == nameof(NodeKind.SteinerInsertion));

        MeshView otherRemoved = editor.RemoveElements([0], []);
        Assert.True(otherRemoved.Succeeded);
        Assert.Single(otherRemoved.Constraints);
        MeshView lastRemoved = editor.RemoveElements([0], []);
        Assert.True(lastRemoved.Succeeded);
        Assert.Empty(lastRemoved.Constraints);
    }

    [Fact]
    public void CrossingInitialLoopDiagonalsCanBothBeRemoved()
    {
        var editor = new EditorMeshModel();
        Assert.True(editor.InsertConstraint(4, 6).Succeeded);
        MeshView crossed = editor.InsertConstraint(5, 7);
        Assert.Single(
            crossed.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion));

        Assert.True(editor.RemoveElements([0], []).Succeeded);
        Assert.True(editor.Undo().Succeeded);
        Assert.True(editor.Redo().Succeeded);
        Assert.True(editor.Undo().Succeeded);
        Assert.True(editor.RemoveElements([0], []).Succeeded);
        MeshView removed = editor.RemoveElements([0], []);

        Assert.True(removed.Succeeded);
        Assert.Empty(removed.Constraints);
        Assert.True(editor.Undo().Succeeded);
        Assert.True(editor.Undo().Succeeded);
        Assert.True(editor.Redo().Succeeded);
        MeshView redone = editor.Redo();
        Assert.True(redone.Succeeded);
        Assert.Empty(redone.Constraints);
    }

    [Fact]
    public void CrossingConstraintsCanBeRemovedTogetherAndReplayed()
    {
        var editor = new EditorMeshModel();
        Assert.True(editor.InsertConstraint(4, 6).Succeeded);
        MeshView crossed = editor.InsertConstraint(5, 7);
        Assert.Single(crossed.Nodes, node => node.Kind == nameof(NodeKind.SteinerInsertion));

        MeshView removed = editor.RemoveElements([0, 1], []);

        Assert.True(removed.Succeeded);
        Assert.Empty(removed.Constraints);
        MeshView restored = editor.Undo();
        Assert.True(restored.Succeeded);
        Assert.Equal(2, restored.Constraints.Count);
        Assert.Single(restored.Nodes, node => node.Kind == nameof(NodeKind.SteinerInsertion));
        MeshView redone = editor.Redo();
        Assert.True(redone.Succeeded);
        Assert.Empty(redone.Constraints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EitherCrossingConstraintCanBeRemovedAfterDeleteAndUndo(int constraintToDelete)
    {
        var editor = new EditorMeshModel();
        Assert.True(editor.InsertConstraint(4, 6).Succeeded);
        Assert.True(editor.InsertConstraint(5, 7).Succeeded);

        Assert.True(editor.RemoveConstraint(constraintToDelete).Succeeded);
        MeshView restored = editor.Undo();
        Assert.True(restored.Succeeded);
        Assert.Equal(2, restored.Constraints.Count);

        MeshView removedFirst = editor.RemoveConstraint(0);
        Assert.True(removedFirst.Succeeded);
        Assert.Single(removedFirst.Constraints);
        MeshView removedSecond = editor.RemoveConstraint(0);
        Assert.True(removedSecond.Succeeded);
        Assert.Empty(removedSecond.Constraints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RestoredCrossingConstraintProjectsTheSameEdges(int constraintToDelete)
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int bottom = Insert(editor, 0, -2);
        int top = Insert(editor, 0, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        MeshView crossed = editor.InsertConstraint(bottom, top);
        var expected = ProjectedSegments(crossed);

        Assert.True(editor.RemoveConstraint(constraintToDelete).Succeeded);
        MeshView restored = editor.Undo();

        Assert.Equal(expected, ProjectedSegments(restored));
        Assert.Equal(4, restored.ConstraintEdges.Count);
        foreach (ConstraintView constraint in restored.Constraints)
        {
            EdgeView edge = constraint.Edges[0];
            Assert.NotEmpty(editor.CollectConstraintLine(edge.A, edge.B));
        }
    }

    [Fact]
    public void RemovedConstraintCanBeFreshlyInsertedAndRemovedAgain()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int bottom = Insert(editor, 0, -2);
        int top = Insert(editor, 0, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        Assert.True(editor.InsertConstraint(bottom, top).Succeeded);

        Assert.True(editor.RemoveConstraint(1).Succeeded);
        MeshView withoutCrossing = editor.State();
        Assert.Single(withoutCrossing.Constraints);
        Assert.All(withoutCrossing.Nodes.Where(node => node.Id == bottom || node.Id == top),
            node => Assert.Equal(0, node.ConstraintCount));

        MeshView reinserted = editor.InsertConstraint(bottom, top);
        Assert.True(reinserted.Succeeded);
        Assert.Equal(2, reinserted.Constraints.Count);
        Assert.Single(reinserted.Nodes, node => node.Kind == nameof(NodeKind.SteinerInsertion));

        MeshView removedAgain = editor.RemoveConstraint(1);
        Assert.True(removedAgain.Succeeded);
        Assert.Single(removedAgain.Constraints);
        Assert.DoesNotContain(removedAgain.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion));
        Assert.All(removedAgain.Nodes.Where(node => node.Id == bottom || node.Id == top),
            node => Assert.Equal(0, node.ConstraintCount));
    }

    [Fact]
    public void RepeatedUndoRedoPreservesEveryCrossingConstraintSegment()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int bottom = Insert(editor, 0, -2);
        int top = Insert(editor, 0, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        MeshView inserted = editor.InsertConstraint(bottom, top);
        string[] expected = ProjectedSegments(inserted);
        Assert.Equal(4, expected.Length);

        for (int cycle = 0; cycle < 20; cycle++)
        {
            MeshView removed = editor.Undo();
            Assert.True(removed.Succeeded);
            Assert.Single(removed.Constraints);
            Assert.Single(removed.ConstraintEdges);

            MeshView reinserted = editor.Redo();
            Assert.True(reinserted.Succeeded);
            Assert.Equal(expected, ProjectedSegments(reinserted));
            Assert.Equal(4, reinserted.ConstraintEdges.Count);
            Assert.All(reinserted.Constraints,
                constraint => Assert.Equal(2, constraint.SegmentCount));
        }
    }

    [Fact]
    public void DeleteUndoRunsCompleteConstraintInsertionPipeline()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int bottom = Insert(editor, 0, -2);
        int top = Insert(editor, 0, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        Assert.True(editor.InsertConstraint(bottom, top).Succeeded);

        Assert.True(editor.RemoveConstraint(0).Succeeded);
        MeshView restored = editor.Undo();

        Assert.True(restored.Succeeded, restored.FailureReason);
        Assert.Equal(2, restored.Constraints.Count);
        Assert.Equal(4, restored.ConstraintEdges.Count);
        Assert.All(restored.Constraints, constraint =>
        {
            Assert.Equal(2, constraint.SegmentCount);
            Assert.Equal(2, editor.CollectConstraint(constraint.Id).Count);
        });
        NodeView intersection = Assert.Single(restored.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion));
        Assert.Equal(2, intersection.ConstraintCount);
    }

    [Fact]
    public void RepeatedRemovalUndoPreservesConstraintAcrossMultipleIntersections()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int lowerLeft = Insert(editor, -1, -2);
        int upperLeft = Insert(editor, -1, 2);
        int lowerRight = Insert(editor, 1, -2);
        int upperRight = Insert(editor, 1, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        Assert.True(editor.InsertConstraint(lowerLeft, upperLeft).Succeeded);
        MeshView inserted = editor.InsertConstraint(lowerRight, upperRight);
        string[] expected = ProjectedSegments(inserted);
        Assert.Equal(7, expected.Length);

        Assert.True(editor.RemoveConstraint(0).Succeeded);
        for (int cycle = 0; cycle < 20; cycle++)
        {
            MeshView reinserted = editor.Undo();
            Assert.True(reinserted.Succeeded);
            Assert.Equal(expected, ProjectedSegments(reinserted));
            Assert.Equal(7, reinserted.ConstraintEdges.Count);
            Assert.Equal([2, 2, 3], reinserted.Constraints
                .Select(constraint => constraint.SegmentCount).Order().ToArray());
            Assert.All(reinserted.Constraints, constraint =>
                Assert.Equal(constraint.Edges, editor.CollectConstraint(constraint.Id)));

            MeshView removed = editor.Redo();
            Assert.True(removed.Succeeded);
            Assert.Equal(2, removed.Constraints.Count);
        }
    }

    [Fact]
    public void GroupUndoRestoresIntersectionDependenciesInInsertionOrder()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3, 0);
        int right = Insert(editor, 3, 0);
        int bottom = Insert(editor, 0, -2);
        int top = Insert(editor, 0, 2);
        int branchEnd = Insert(editor, 2, 2);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        MeshView crossed = editor.InsertConstraint(bottom, top);
        int intersection = Assert.Single(crossed.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion)).Id;
        Assert.True(editor.InsertConstraint(intersection, branchEnd).Succeeded);

        MeshView removed = editor.RemoveElements([0, 1, 2], []);
        Assert.True(removed.Succeeded);
        Assert.Empty(removed.Constraints);

        MeshView restored = editor.Undo();
        Assert.True(restored.Succeeded);
        Assert.Equal(3, restored.Constraints.Count);
        Assert.Equal(5, restored.ConstraintEdges.Count);
    }

    [Fact]
    public void RepeatedGroupUndoRedoResolvesGeneratedEndpointsGeometrically()
    {
        var editor = new EditorMeshModel();
        int a = Insert(editor, -3.1, -0.7);
        int b = Insert(editor, 2.8, 1.3);
        int c = Insert(editor, -1.7, 2.4);
        int d = Insert(editor, 1.9, -2.2);
        int branchEnd = Insert(editor, 2.6, 2.5);
        Assert.True(editor.InsertConstraint(a, b).Succeeded);
        MeshView crossed = editor.InsertConstraint(c, d);
        int intersection = Assert.Single(crossed.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion)).Id;
        Assert.True(editor.InsertConstraint(intersection, branchEnd).Succeeded);
        string[] expected = ProjectedSegments(editor.State());

        Assert.True(editor.RemoveElements([0, 1, 2], []).Succeeded);
        for (int cycle = 0; cycle < 20; cycle++)
        {
            MeshView restored = editor.Undo();
            Assert.True(restored.Succeeded);
            Assert.Equal(expected, ProjectedSegments(restored));
            Assert.Equal(3, restored.Constraints.Count);

            MeshView removed = editor.Redo();
            Assert.True(removed.Succeeded);
            Assert.Empty(removed.Constraints);
        }
    }

    [Fact]
    public void CompletePointAndConstraintHistoryCanBeReplayedRepeatedly()
    {
        var editor = new EditorMeshModel();
        int left = Insert(editor, -3.2, .1);
        int right = Insert(editor, 3.1, -.2);
        int bottom = Insert(editor, .2, -2.3);
        int top = Insert(editor, -.1, 2.4);
        Assert.True(editor.InsertConstraint(left, right).Succeeded);
        MeshView complete = editor.InsertConstraint(bottom, top);
        AssertCrossingState(complete);

        for (int cycle = 0; cycle < 20; cycle++)
        {
            for (int action = 0; action < 6; action++)
                Assert.True(editor.Undo().Succeeded);
            MeshView empty = editor.State();
            Assert.Empty(empty.Constraints);
            Assert.Equal(4, empty.Nodes.Count(node => !node.IsSuper));

            MeshView replayed = empty;
            for (int action = 0; action < 6; action++)
            {
                replayed = editor.Redo();
                Assert.True(replayed.Succeeded);
            }
            AssertCrossingState(replayed);
        }
    }

    static void AssertCrossingState(MeshView view)
    {
        Assert.Equal(2, view.Constraints.Count);
        Assert.All(view.Constraints, constraint => Assert.Equal(2, constraint.SegmentCount));
        Assert.Equal(4, view.ConstraintEdges.Count);
        NodeView intersection = Assert.Single(view.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion));
        Assert.Equal(2, intersection.ConstraintCount);
    }

    [Fact]
    public void RemovingConstrainedNodeExplainsConstraintCount()
    {
        var editor = new EditorMeshModel();
        int a = Insert(editor, -2, 0);
        int b = Insert(editor, 2, 0);
        Assert.True(editor.InsertConstraint(a, b).Succeeded);

        MeshView failed = editor.Remove(a);

        Assert.False(failed.Succeeded);
        Assert.Contains("ConstraintCount is 1", failed.FailureReason);
    }

    [Fact]
    public void RemovingConstrainedGeneratedIntersectionIsRejected()
    {
        var editor = new EditorMeshModel();
        Assert.True(editor.InsertConstraint(4, 6).Succeeded);
        MeshView crossed = editor.InsertConstraint(5, 7);
        int intersection = Assert.Single(crossed.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion)).Id;

        MeshView failed = editor.RemoveElements([], [intersection]);

        Assert.False(failed.Succeeded);
        Assert.Contains("ConstraintCount", failed.FailureReason);
    }

    [Fact]
    public void NonStructuralSteinerInsertionCanBeRemovedFromConstraint()
    {
        var editor = new EditorMeshModel();
        int start = Insert(editor, -2, 0);
        int middle = Insert(editor, 0, 0);
        int end = Insert(editor, 2, 0);
        Assert.True(editor.InsertConstraint(start, end).Succeeded);
        Assert.Equal(nameof(NodeKind.SteinerInsertion),
            Assert.Single(editor.State().Nodes, node => node.Id == middle).Kind);

        MeshView removed = editor.RemoveElements([], [middle]);

        Assert.True(removed.Succeeded, removed.FailureReason);
        Assert.DoesNotContain(removed.Nodes, node => node.Id == middle);
        ConstraintView constraint = Assert.Single(removed.Constraints);
        Assert.Equal(1, constraint.SegmentCount);
    }

    [Fact]
    public void UnconstrainedRefinementSteinerCanBeRemoved()
    {
        var editor = new EditorMeshModel();
        MeshView inserted = editor.Insert(0, 0);
        int id = Assert.IsType<int>(inserted.ChangedNodeId);
        var ids = Assert.IsType<Dictionary<Node, int>>(
            typeof(EditorMeshModel)
                .GetField("_ids", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .GetValue(editor));
        Node node = ids.Single(pair => pair.Value == id).Key;
        typeof(Node).GetProperty(nameof(Node.Kind))!
            .SetValue(node, NodeKind.SteinerRefinement);

        MeshView removed = editor.RemoveElements([], [id]);

        Assert.True(removed.Succeeded, removed.FailureReason);
        Assert.DoesNotContain(removed.Nodes, candidate => candidate.Id == id);
    }

    static string[] ProjectedSegments(MeshView view)
    {
        var positions = view.Nodes.ToDictionary(node => node.Id, node => (node.X, node.Y));
        return view.Constraints.SelectMany(constraint => constraint.Edges.Select(edge =>
        {
            (double X, double Y) a = positions[edge.A];
            (double X, double Y) b = positions[edge.B];
            return string.CompareOrdinal(a.ToString(), b.ToString()) <= 0
                ? $"{a}-{b}" : $"{b}-{a}";
        })).Order().ToArray();
    }

    static int Insert(EditorMeshModel editor, double x, double y)
    {
        MeshView result = editor.Insert(x, y);
        Assert.True(result.Succeeded);
        return Assert.IsType<int>(result.ChangedNodeId);
    }
}
