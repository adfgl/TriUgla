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

    static int Insert(EditorMeshModel editor, double x, double y)
    {
        MeshView result = editor.Insert(x, y);
        Assert.True(result.Succeeded);
        return Assert.IsType<int>(result.ChangedNodeId);
    }
}
