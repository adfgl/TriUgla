namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    int Id(Node node)
    {
        if (_ids.TryGetValue(node, out int id)) return id;
        id = _nextId++;
        _ids.Add(node, id);
        return id;
    }

    Node? LiveNode(int id)
        => _ids.FirstOrDefault(pair => pair.Value == id && !pair.Key.Dead).Key;

    PointHandle Handle(Node node)
    {
        int id = Id(node);
        if (_handles.TryGetValue(id, out PointHandle? handle)) return handle;
        handle = new PointHandle(node.Position, id);
        _handles[id] = handle;
        return handle;
    }

    internal bool InsertPoint(PointHandle point, int? loopIndex)
    {
        if (loopIndex is not null && !ReleaseInitialLoop()) return false;
        InsertNodeResult result = _mesher.Insert(point.Position);
        bool inserted = result.Status is InsertNodeStatus.InsertedIntoFace or InsertNodeStatus.InsertedIntoEdge;
        if (!inserted || result.Node is null) return false;
        int id = Id(result.Node);
        point.NodeId = id;
        _handles[id] = point;
        if (loopIndex is int index)
        {
            _loopPoints.Insert(Math.Min(index, _loopPoints.Count), point);
            return RebuildInitialLoop();
        }
        return true;
    }

    internal bool RemovePoint(PointHandle point, int? loopIndex)
    {
        Node? node = LiveNode(point.NodeId);
        if (node is null) return false;
        if (loopIndex is not null && !ReleaseInitialLoop()) return false;
        bool removed = _mesher.Remove(node).Removed;
        if (removed && loopIndex is not null)
        {
            _loopPoints.Remove(point);
            return RebuildInitialLoop();
        }
        return removed;
    }

    internal bool InsertConstraintHandle(ConstraintHandle handle)
    {
        var spans = new List<ConstraintSpan>(handle.Spans.Count);
        foreach ((PointHandle fromHandle, PointHandle toHandle) in handle.Spans)
        {
            Node? from = LiveNode(fromHandle.NodeId);
            Node? to = LiveNode(toHandle.NodeId);
            if (from is null || to is null) return false;
            spans.Add(new ConstraintSpan(from, to));
        }

        var constraint = new Constraint(spans: spans, name: handle.Name);
        if (!_mesher.TryInsertConstraint(constraint, out _)) return false;
        handle.Current = constraint;
        return true;
    }

    internal bool RemoveConstraintHandle(ConstraintHandle handle)
        => handle.Current is not null && _mesher.TryRemoveConstraint(handle.Current, out _);

    internal bool InsertLoopHandle(LoopHandle handle)
    {
        Node[] nodes = handle.Points.Select(point => LiveNode(point.NodeId)).OfType<Node>().ToArray();
        if (nodes.Length != handle.Points.Count) return false;
        var loop = new Loop(nodes, handle.Name);
        if (!_mesher.TryInsertLoop(loop, out _)) return false;
        try { Classify(); }
        catch (InvalidOperationException)
        {
            _mesher.TryRemoveLoop(loop, out _);
            return false;
        }
        handle.Current = loop;
        return true;
    }

    internal bool RemoveLoopHandle(LoopHandle handle)
        => handle.Current is not null && _mesher.TryRemoveLoop(handle.Current, out _);

    bool ReleaseInitialLoop()
    {
        if (_initialLoop is null) return true;
        if (!_mesher.TryRemoveLoop(_initialLoop, out _)) return false;
        _initialLoop = null;
        return true;
    }

    bool RebuildInitialLoop()
    {
        if (_loopPoints.Count < 3)
        {
            _initialLoop = null;
            return true;
        }
        Node[] nodes = _loopPoints.Select(point => LiveNode(point.NodeId)!).ToArray();
        var loop = new Loop(nodes, "Initial rectangle");
        if (!_mesher.TryInsertLoop(loop, out _)) return false;
        _initialLoop = loop;
        return true;
    }
}
