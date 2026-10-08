namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    public MeshView Undo()
    {
        if (!_undo.TryPop(out IEditorCommand? command)) return Snapshot(false, null);
        if (!command.Undo())
        {
            _undo.Push(command);
            return Snapshot(false, null);
        }
        _redo.Push(command);
        return Snapshot(true, command.ChangedNodeId);
    }

    public MeshView Redo()
    {
        if (!_redo.TryPop(out IEditorCommand? command)) return Snapshot(false, null);
        if (!command.Execute())
        {
            _redo.Push(command);
            return Snapshot(false, null);
        }
        _undo.Push(command);
        return Snapshot(true, command.ChangedNodeId);
    }

    bool Execute(IEditorCommand command)
    {
        if (!command.Execute()) return false;
        _undo.Push(command);
        _redo.Clear();
        return true;
    }
}
