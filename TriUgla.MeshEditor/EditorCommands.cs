namespace TriUgla.MeshEditor;

internal interface IEditorCommand
{
    int? ChangedNodeId { get; }
    bool Execute();
    bool Undo();
}

internal sealed class CompositeEditorCommand(IEnumerable<IEditorCommand> commands) : IEditorCommand
{
    readonly IEditorCommand[] _commands = commands.ToArray();

    public int? ChangedNodeId => null;

    public bool Execute()
    {
        int completed = 0;
        while (completed < _commands.Length && _commands[completed].Execute()) completed++;
        if (completed == _commands.Length) return true;
        while (completed > 0) _commands[--completed].Undo();
        return false;
    }

    public bool Undo()
    {
        int index = _commands.Length - 1;
        for (; index >= 0 && _commands[index].Undo(); index--) { }
        if (index < 0) return true;
        for (int restore = index + 1; restore < _commands.Length; restore++)
            _commands[restore].Execute();
        return false;
    }
}

internal sealed class InsertNodeCommand(EditorMeshModel model, Vec2 position, int? loopIndex = null)
    : IEditorCommand
{
    public int? ChangedNodeId => model.NodeIdAt(position);
    public bool Execute() => model.InsertPoint(position, loopIndex);
    public bool Undo() => model.RemovePoint(position, loopIndex);
}

internal sealed class RemoveNodeCommand(EditorMeshModel model, Vec2 position, int? loopIndex = null)
    : IEditorCommand
{
    public int? ChangedNodeId => model.NodeIdAt(position);
    public int? LoopIndex => loopIndex;
    public bool Execute() => model.RemovePoint(position, loopIndex);
    public bool Undo() => model.InsertPoint(position, loopIndex);
}

internal sealed class InsertConstraintCommand(EditorMeshModel model, ConstraintHandle constraint)
    : IEditorCommand
{
    public int? ChangedNodeId => null;
    public bool Execute() => model.InsertConstraintHandle(constraint);
    public bool Undo() => model.RemoveConstraintHandle(constraint);
}

internal sealed class RemoveConstraintCommand(EditorMeshModel model, ConstraintHandle constraint)
    : IEditorCommand
{
    public int? ChangedNodeId => null;
    public bool Execute() => model.RemoveConstraintHandle(constraint);
    public bool Undo() => model.InsertConstraintHandle(constraint);
}

internal sealed class InsertLoopCommand(EditorMeshModel model, LoopHandle loop) : IEditorCommand
{
    public int? ChangedNodeId => null;
    public bool Execute() => model.InsertLoopHandle(loop);
    public bool Undo() => model.RemoveLoopHandle(loop);
}

internal sealed class ConstraintHandle(
    string? name,
    IEnumerable<ConstraintPathHandle> paths,
    IEnumerable<ConstraintPointHandle>? points = null)
{
    public string? Name { get; } = name;
    public List<ConstraintPathHandle> Paths { get; } = paths.ToList();
    public List<ConstraintPointHandle> Points { get; } = points?.ToList() ?? [];

    public static ConstraintHandle From(Constraint constraint)
        => new(
            constraint.Name,
            constraint.Spans.Select(span =>
                new ConstraintPathHandle([span.From.Position, span.To.Position])),
            constraint.Points.Select(point => new ConstraintPointHandle(point.Node.Position, point.Name)));
}

internal sealed record ConstraintPathHandle(IReadOnlyList<Vec2> Points);
internal sealed record ConstraintPointHandle(Vec2 Position, string? Name);

internal sealed class LoopHandle(
    string? name,
    IReadOnlyList<Vec2> points)
{
    public string? Name { get; } = name;
    public IReadOnlyList<Vec2> Points { get; } = points;
}
