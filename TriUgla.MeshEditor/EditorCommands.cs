namespace TriUgla.MeshEditor;

internal interface IEditorCommand
{
    int? ChangedNodeId { get; }
    bool Execute();
    bool Undo();
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
    {
        var result = new ConstraintHandle(
            constraint.Name,
            [],
            constraint.Points.Select(point => new ConstraintPointHandle(handle(point.Node), point.Name)),
            constraint);
        result.CapturePaths(constraint, handle);
        return result;
    }

    public void CapturePaths(Constraint constraint, Func<Node, PointHandle> handle)
    {
        Paths.Clear();
        foreach (ConstraintSpan span in constraint.Spans)
        {
            // Generated Steiner nodes belong to the inserted representation, not
            // to command history. Redo reconstructs them from the authored endpoints.
            Paths.Add(new ConstraintPathHandle([handle(span.From), handle(span.To)]));
        }
    }
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
