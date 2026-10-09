using TriUgla.MeshEditor;

namespace TriUgla.Tests;

public class EditorPolylineTests
{
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
    public void RemovingGeneratedIntersectionExplainsHowToDeleteIt()
    {
        var editor = new EditorMeshModel();
        Assert.True(editor.InsertConstraint(4, 6).Succeeded);
        MeshView crossed = editor.InsertConstraint(5, 7);
        int intersection = Assert.Single(crossed.Nodes,
            node => node.Kind == nameof(NodeKind.SteinerInsertion)).Id;

        MeshView failed = editor.RemoveElements([], [intersection]);

        Assert.False(failed.Succeeded);
        Assert.Contains("remove their constraints instead", failed.FailureReason);
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
