namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    static readonly (double X, double Y)[] InitialVertices =
    [
        (-4, -3), (4, -3), (4, 3), (-4, 3)
    ];

    readonly Dictionary<Node, int> _ids = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<EditorConstraint, ConstraintHandle> _constraintDefinitions =
        new(ReferenceEqualityComparer.Instance);
    readonly List<EditorConstraint> _constraints = [];
    readonly Stack<IEditorCommand> _undo = new();
    readonly Stack<IEditorCommand> _redo = new();
    readonly List<Vec2> _loopPoints = [];
    Mesher _mesher = null!;
    Loop? _initialLoop;
    string? _failureReason;
    int _nextId;

    internal void ClearFailure() => _failureReason = null;
    internal void Fail(string? reason) => _failureReason = reason ?? "The mesh operation failed.";

    ConstraintHandle Definition(EditorConstraint constraint)
        => _constraintDefinitions.TryGetValue(constraint, out ConstraintHandle? definition)
            ? definition
            : ConstraintHandle.From(constraint);

    public EditorMeshModel() => Reset();

    public MeshView State() => Snapshot(true, null);

    public MeshView Insert(double x, double y)
    {
        var command = new InsertNodeCommand(this, new Vec2(x, y));
        bool inserted = Execute(command);
        return Snapshot(inserted, command.ChangedNodeId);
    }

    public MeshView Remove(int nodeId)
    {
        RemoveNodeCommand? command = CreateRemoveNodeCommand(nodeId);
        if (command is null)
        {
            Fail($"Node {nodeId} is not present in the current mesh.");
            return Snapshot(false, null);
        }
        bool removed = Execute(command);
        return Snapshot(removed, null);
    }

    public MeshView UpdateNodeData(int nodeId, double elevation, double targetArea)
    {
        Node? node = LiveNode(nodeId);
        if (node is null)
        {
            Fail($"Node {nodeId} is not present in the current mesh.");
            return Snapshot(false, null);
        }
        if (!double.IsFinite(elevation) || !double.IsFinite(targetArea) || targetArea < 0d)
        {
            Fail("Elevation must be finite and target area must be finite and non-negative.");
            return Snapshot(false, nodeId);
        }

        var data = new NodeData(elevation, targetArea);
        if (node.Data == data) return Snapshot(true, nodeId);
        bool updated = Execute(new UpdateNodeDataCommand(this, node.Position, node.Data, data));
        return Snapshot(updated, nodeId);
    }

    public MeshView UpdateNodeProperty(
        IReadOnlyList<int> nodeIds,
        string propertyName,
        double value)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);
        int[] distinctIds = nodeIds.Distinct().ToArray();
        Node[] nodes = distinctIds.Select(LiveNode).OfType<Node>().ToArray();
        if (distinctIds.Length == 0 || nodes.Length != distinctIds.Length)
        {
            Fail("One or more selected nodes are no longer present in the current mesh.");
            return Snapshot(false, null);
        }
        if (!double.IsFinite(value) || propertyName == "Target area" && value < 0d)
        {
            Fail("Elevation must be finite and target area must be finite and non-negative.");
            return Snapshot(false, null);
        }
        if (propertyName is not ("Elevation" or "Target area"))
        {
            Fail($"Node property '{propertyName}' is not editable.");
            return Snapshot(false, null);
        }

        var commands = new List<IEditorCommand>(nodes.Length);
        foreach (Node node in nodes)
        {
            NodeData after = propertyName == "Elevation"
                ? node.Data with { Elevation = value }
                : node.Data with { Area = value };
            if (node.Data != after)
                commands.Add(new UpdateNodeDataCommand(this, node.Position, node.Data, after));
        }
        if (commands.Count == 0) return Snapshot(true, distinctIds[0]);
        bool updated = Execute(new CompositeEditorCommand(commands));
        return Snapshot(updated, distinctIds.Length == 1 ? distinctIds[0] : null);
    }

    public MeshView RefineFaces(
        IReadOnlyList<int> faceIds,
        bool refineLand = true,
        bool refineLakes = false)
    {
        ArgumentNullException.ThrowIfNull(faceIds);
        Face[] faces = _mesher.Traversal.Faces().Where(face => !face.Dead).ToArray();
        int[] distinctIds = faceIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            Fail("Select at least one face to refine.");
            return Snapshot(false, null);
        }
        if (distinctIds.Any(id => id < 0 || id >= faces.Length))
        {
            Fail("One or more selected faces are no longer present in the current mesh.");
            return Snapshot(false, null);
        }

        try
        {
            RefineResult result = _mesher.RefineDetailed(
                distinctIds.Select(id => faces[id]),
                new FaceRanker(),
                RefinementSettings(refineLand, refineLakes));
            if (!result.Completed) return RefinementFailed(result);
            return Snapshot(true, null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Fail($"Selected-face refinement failed: {exception.Message}");
            return Snapshot(false, null);
        }
    }

    public MeshView RefineAll(bool refineLand = true, bool refineLakes = false)
    {
        try
        {
            RefineResult result = _mesher.RefineDetailed(
                new FaceRanker(), RefinementSettings(refineLand, refineLakes));
            if (!result.Completed) return RefinementFailed(result);
            return Snapshot(true, null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Fail($"Mesh refinement failed: {exception.Message}");
            return Snapshot(false, null);
        }
    }

    static RefineSettings RefinementSettings(bool refineLand, bool refineLakes)
        => RefineSettings.Default with
        {
            RefineLand = refineLand,
            RefineLakes = refineLakes
        };

    MeshView RefinementFailed(RefineResult result)
    {
        Fail(result.FailureReason ?? $"Refinement stopped with status {result.Status}.");
        return Snapshot(false, null);
    }

    public MeshView RemoveElements(
        IReadOnlyList<int> constraintIds,
        IReadOnlyList<int> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(constraintIds);
        ArgumentNullException.ThrowIfNull(nodeIds);
        int[] distinctConstraintIds = constraintIds.Distinct().ToArray();
        if (distinctConstraintIds.Any(id => id < 0 || id >= _constraints.Count))
        {
            Fail("One or more selected constraints are no longer present in the current mesh.");
            return Snapshot(false, null);
        }
        (int Index, EditorConstraint Constraint)[] constraints = distinctConstraintIds
            .Select(id => (Index: id, Constraint: _constraints[id]))
            .ToArray();

        int[] distinctNodeIds = nodeIds.Distinct().ToArray();
        Node[] selectedNodes = distinctNodeIds
            .Select(id => _ids.FirstOrDefault(pair => pair.Value == id && !pair.Key.Dead).Key)
            .OfType<Node>()
            .ToArray();
        if (selectedNodes.Length != distinctNodeIds.Length)
        {
            Fail("One or more selected nodes are no longer present in the current mesh.");
            return Snapshot(false, null);
        }

        RemoveNodeCommand[] nodes = selectedNodes
            .Where(node => node.Kind != NodeKind.Super)
            .Select(node => CreateRemoveNodeCommand(
                Id(node), node.Kind == NodeKind.SteinerInsertion))
            .OfType<RemoveNodeCommand>()
            .OrderByDescending(command => command.LoopIndex ?? -1)
            .ToArray();

        IEditorCommand[] commands = constraints
            .OrderByDescending(item => item.Index)
            .Select(item => (IEditorCommand)new RemoveConstraintCommand(
                this, Definition(item.Constraint)))
            .Concat(nodes)
            .ToArray();
        if (commands.Length == 0)
        {
            Fail("Super-structure nodes cannot be removed.");
            return Snapshot(false, null);
        }
        bool removed = Execute(new CompositeEditorCommand(commands));
        return Snapshot(removed, null);
    }

    RemoveNodeCommand? CreateRemoveNodeCommand(int nodeId, bool allowAlreadyRemoved = false)
    {
        Node? node = _ids.FirstOrDefault(pair => pair.Value == nodeId).Key;
        if (node is null || node.Dead) return null;
        int loopIndex = _loopPoints.IndexOf(node.Position);
        return new RemoveNodeCommand(
            this, node.Position, loopIndex >= 0 ? loopIndex : null, allowAlreadyRemoved);
    }

    public MeshView InsertConstraint(int fromId, int toId)
    {
        Node? from = LiveNode(fromId);
        Node? to = LiveNode(toId);
        if (from is null || to is null || ReferenceEquals(from, to)) return Snapshot(false, null);

        var handle = new ConstraintHandle(
            $"Constraint {fromId}-{toId}",
            [new ConstraintPathHandle([from.Position, to.Position])]);
        bool inserted = Execute(new InsertConstraintCommand(this, handle));
        return Snapshot(inserted, null);
    }

    public MeshView InsertPolyline(IReadOnlyList<int> nodeIds)
    {
        if (nodeIds.Count < 2) return Snapshot(false, null);
        Node[] nodes = nodeIds.Select(LiveNode).OfType<Node>().ToArray();
        if (nodes.Length != nodeIds.Count || nodes.Zip(nodes.Skip(1)).Any(pair => ReferenceEquals(pair.First, pair.Second)))
            return Snapshot(false, null);

        var polyline = new Polyline(nodes, $"Polyline {_constraints.Count + 1}");
        var handle = new ConstraintHandle(
            polyline.Name,
            polyline.Nodes.Zip(polyline.Nodes.Skip(1),
                (from, to) => new ConstraintPathHandle([from.Position, to.Position])).ToArray());
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
            $"Polygon {_mesher.Constraints.Loops.Count + 1}",
            nodes.Select(node => node.Position).ToArray());
        bool inserted = Execute(new InsertLoopCommand(this, handle));
        return Snapshot(inserted, null);
    }

    public IReadOnlyList<EdgeView> CollectConstraintLine(int startId, int endId)
    {
        EditorConstraint? constraint = FindConstraint(startId, endId);
        if (constraint is null) return [];
        return ConstraintEdges(constraint);
    }

    public IReadOnlyList<EdgeView> CollectConstraint(int constraintId)
    {
        if (constraintId < 0 || constraintId >= _constraints.Count) return [];
        return ConstraintEdges(_constraints[constraintId]);
    }

    public MeshView RemoveConstraint(int startId, int endId)
    {
        EditorConstraint? constraint = FindConstraint(startId, endId);
        if (constraint is null)
        {
            Fail($"No constraint contains edge {startId}-{endId} in the current mesh.");
            return Snapshot(false, null);
        }
        return RemoveConstraint(constraint);
    }

    public MeshView RemoveConstraint(int constraintId)
    {
        if (constraintId < 0 || constraintId >= _constraints.Count)
        {
            Fail($"Constraint {constraintId} is not present in the current mesh.");
            return Snapshot(false, null);
        }
        return RemoveConstraint(_constraints[constraintId]);
    }

    MeshView RemoveConstraint(EditorConstraint constraint)
    {
        ConstraintHandle handle = Definition(constraint);
        bool removed = Execute(new RemoveConstraintCommand(this, handle));
        return Snapshot(removed, null);
    }

    public MeshView Reset()
    {
        _ids.Clear();
        _constraintDefinitions.Clear();
        _constraints.Clear();
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
            _loopPoints.Add(node.Position);
        }
        _initialLoop = new Loop(loopNodes, "Initial rectangle");
        if (!_mesher.TryInsertLoop(_initialLoop, out string? reason))
            throw new InvalidOperationException($"Could not insert initial loop: {reason}");
        return Snapshot(true, null);
    }

}
