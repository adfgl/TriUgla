using TriUgla.MeshEditor;

namespace TriUgla.Tests;

public class EditorPolylineTests
{
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

    static int Insert(EditorMeshModel editor, double x, double y)
    {
        MeshView result = editor.Insert(x, y);
        Assert.True(result.Succeeded);
        return Assert.IsType<int>(result.ChangedNodeId);
    }
}
