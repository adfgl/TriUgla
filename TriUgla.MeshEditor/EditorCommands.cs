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

internal sealed class InsertNodeCommand(EditorMeshModel model, PointHandle point, int? loopIndex = null)
    : IEditorCommand
{
    public int? ChangedNodeId => point.NodeId >= 0 ? point.NodeId : null;
    public bool Execute() => model.InsertPoint(point, loopIndex);
    public bool Undo() => model.RemovePoint(point, loopIndex);
}

internal sealed class RemoveNodeCommand(EditorMeshModel model, PointHandle point, int? loopIndex = null)
    : IEditorCommand
{
    public int? ChangedNodeId => point.NodeId;
    public int? LoopIndex => loopIndex;
    public bool Execute() => model.RemovePoint(point, loopIndex);
    public bool Undo() => model.InsertPoint(point, loopIndex);
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

internal sealed class PointHandle(Vec2 position, int nodeId = -1)
{
    public Vec2 Position { get; } = position;
    public int NodeId { get; set; } = nodeId;
}

internal sealed class ConstraintHandle(
    string? name,
    IEnumerable<ConstraintPathHandle> paths,
    IEnumerable<ConstraintPointHandle>? points = null,
    Constraint? current = null)
{
    public string? Name { get; } = name;
    public List<ConstraintPathHandle> Paths { get; } = paths.ToList();
    public List<ConstraintPointHandle> Points { get; } = points?.ToList() ?? [];
    public Constraint? Current { get; set; } = current;

    public static ConstraintHandle From(Constraint constraint, Func<Node, PointHandle> handle)
        => new(
            constraint.Name,
            constraint.Spans.Select(span =>
                new ConstraintPathHandle([handle(span.From), handle(span.To)])),
            constraint.Points.Select(point => new ConstraintPointHandle(handle(point.Node), point.Name)),
            constraint);
}

internal sealed record ConstraintPathHandle(IReadOnlyList<PointHandle> Points);
internal sealed record ConstraintPointHandle(PointHandle Node, string? Name);

internal sealed class LoopHandle(
    string? name,
    IReadOnlyList<PointHandle> points,
    Loop? current = null)
{
    public string? Name { get; } = name;
    public IReadOnlyList<PointHandle> Points { get; } = points;
    public Loop? Current { get; set; } = current;
}
