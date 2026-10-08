namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    static readonly (double X, double Y)[] InitialVertices =
    [
        (-4, -3), (4, -3), (4, 3), (-4, 3)
    ];

    readonly Dictionary<Node, int> _ids = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<int, PointHandle> _handles = new();
    readonly Dictionary<Constraint, ConstraintHandle> _constraintHandles = new(ReferenceEqualityComparer.Instance);
    readonly Stack<IEditorCommand> _undo = new();
    readonly Stack<IEditorCommand> _redo = new();
    readonly List<PointHandle> _loopPoints = [];
    Mesher _mesher = null!;
    Loop? _initialLoop;
    int _nextId;

    public EditorMeshModel() => Reset();

    public MeshView State() => Snapshot(true, null);

    public MeshView Insert(double x, double y)
    {
        var point = new PointHandle(new Vec2(x, y));
        var command = new InsertNodeCommand(this, point);
        bool inserted = Execute(command);
        return Snapshot(inserted, command.ChangedNodeId);
    }

    public MeshView Remove(int nodeId)
    {
        RemoveNodeCommand? command = CreateRemoveNodeCommand(nodeId);
        if (command is null) return Snapshot(false, null);
        bool removed = Execute(command);
        return Snapshot(removed, null);
    }

    public MeshView RemoveElements(
        IReadOnlyList<int> constraintIds,
        IReadOnlyList<int> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(constraintIds);
        ArgumentNullException.ThrowIfNull(nodeIds);
        int[] distinctConstraintIds = constraintIds.Distinct().ToArray();
        Constraint[] constraints = distinctConstraintIds
            .Select(id => id >= 0 && id < _mesher.Constraints.Count
                ? _mesher.Constraints[id]
                : null)
            .OfType<Constraint>()
            .ToArray();
        if (constraints.Length != distinctConstraintIds.Length) return Snapshot(false, null);

        int[] distinctNodeIds = nodeIds.Distinct().ToArray();
        Node[] selectedNodes = distinctNodeIds
            .Select(id => _ids.FirstOrDefault(pair => pair.Value == id && !pair.Key.Dead).Key)
            .OfType<Node>()
            .ToArray();
        if (selectedNodes.Length != distinctNodeIds.Length) return Snapshot(false, null);

        RemoveNodeCommand[] nodes = selectedNodes
            .Where(node => node.Kind is not NodeKind.SteinerInsertion and not NodeKind.SteinerRefinement)
            .Select(node => CreateRemoveNodeCommand(Id(node)))
            .OfType<RemoveNodeCommand>()
            .OrderByDescending(command => command.LoopIndex ?? -1)
            .ToArray();

        IEditorCommand[] commands = constraints
            .Select(constraint => (IEditorCommand)new RemoveConstraintCommand(this, Handle(constraint)))
            .Concat(nodes)
            .ToArray();
        if (commands.Length == 0) return Snapshot(false, null);
        bool removed = Execute(new CompositeEditorCommand(commands));
        return Snapshot(removed, null);
    }

    RemoveNodeCommand? CreateRemoveNodeCommand(int nodeId)
    {
        Node? node = _ids.FirstOrDefault(pair => pair.Value == nodeId).Key;
        if (node is null || node.Dead) return null;
        if (!_handles.TryGetValue(nodeId, out PointHandle? point))
        {
            point = new PointHandle(node.Position, nodeId);
            _handles[nodeId] = point;
        }
        int loopIndex = _loopPoints.IndexOf(point);
        return new RemoveNodeCommand(this, point, loopIndex >= 0 ? loopIndex : null);
    }

    public MeshView InsertConstraint(int fromId, int toId)
    {
        Node? from = LiveNode(fromId);
        Node? to = LiveNode(toId);
        if (from is null || to is null || ReferenceEquals(from, to)) return Snapshot(false, null);

        var handle = new ConstraintHandle(
            $"Constraint {fromId}-{toId}",
            [new ConstraintPathHandle([Handle(from), Handle(to)])]);
        bool inserted = Execute(new InsertConstraintCommand(this, handle));
        return Snapshot(inserted, null);
    }

    public MeshView InsertPolyline(IReadOnlyList<int> nodeIds)
    {
        if (nodeIds.Count < 2) return Snapshot(false, null);
        Node[] nodes = nodeIds.Select(LiveNode).OfType<Node>().ToArray();
        if (nodes.Length != nodeIds.Count || nodes.Zip(nodes.Skip(1)).Any(pair => ReferenceEquals(pair.First, pair.Second)))
            return Snapshot(false, null);

        var polyline = new Polyline(nodes, $"Polyline {_mesher.Constraints.Count + 1}");
        var handle = new ConstraintHandle(
            polyline.Name,
            polyline.Nodes.Zip(polyline.Nodes.Skip(1),
                (from, to) => new ConstraintPathHandle([Handle(from), Handle(to)])).ToArray());
        bool inserted = Execute(new InsertConstraintCommand(this, handle));
        return Snapshot(inserted, null);
    }

    public MeshView InsertPolygon(IReadOnlyList<int> nodeIds)
    {
        if (nodeIds.Count < 3 || nodeIds.Distinct().Count() != nodeIds.Count)
            return Snapshot(false, null);
        Node[] nodes = nodeIds.Select(LiveNode).OfType<Node>().ToArray();
        if (nodes.Length != nodeIds.Count) return Snapshot(false, null);

        var handle = new LoopHandle(
            $"Polygon {_mesher.Loops.Count + 1}",
            nodes.Select(Handle).ToArray());
        bool inserted = Execute(new InsertLoopCommand(this, handle));
        return Snapshot(inserted, null);
    }

    public IReadOnlyList<EdgeView> CollectConstraintLine(int startId, int endId)
    {
        Constraint? constraint = FindConstraint(startId, endId);
        if (constraint is null) return [];
        return ConstraintEdges(constraint);
    }

    public MeshView RemoveConstraint(int startId, int endId)
    {
        Constraint? constraint = FindConstraint(startId, endId);
        if (constraint is null) return Snapshot(false, null);
        return RemoveConstraint(constraint);
    }

    public MeshView RemoveConstraint(int constraintId)
    {
        if (constraintId < 0 || constraintId >= _mesher.Constraints.Count)
            return Snapshot(false, null);
        return RemoveConstraint(_mesher.Constraints[constraintId]);
    }

    MeshView RemoveConstraint(Constraint constraint)
    {
        ConstraintHandle handle = Handle(constraint);
        bool removed = Execute(new RemoveConstraintCommand(this, handle));
        return Snapshot(removed, null);
    }

    public MeshView Reset()
    {
        _ids.Clear();
        _handles.Clear();
        _constraintHandles.Clear();
        _undo.Clear();
        _redo.Clear();
        _loopPoints.Clear();
        _nextId = 0;
        _mesher = new Mesher(new Vec2(-6, -4.5), new Vec2(6, 4.5), superStructureSideCount: 4);
        foreach (Node node in _mesher.SuperStructure!.Nodes)
        {
            Id(node);
        }
        var loopNodes = new List<Node>(InitialVertices.Length);
        foreach ((double x, double y) in InitialVertices)
        {
            InsertNodeResult result = _mesher.Insert(new Vec2(x, y));
            Node node = result.Node ?? throw new InvalidOperationException(
                $"Could not insert initial loop node at ({x}, {y}).");
            Id(node);
            loopNodes.Add(node);
            var point = new PointHandle(node.Position, Id(node));
            _handles[point.NodeId] = point;
            _loopPoints.Add(point);
        }
        _initialLoop = new Loop(loopNodes, "Initial rectangle");
        if (!_mesher.TryInsertLoop(_initialLoop, out string? reason))
            throw new InvalidOperationException($"Could not insert initial loop: {reason}");
        return Snapshot(true, null);
    }

}
