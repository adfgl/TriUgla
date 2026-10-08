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

    ConstraintHandle Handle(Constraint constraint)
    {
        if (_constraintHandles.TryGetValue(constraint, out ConstraintHandle? handle)) return handle;
        handle = ConstraintHandle.From(constraint, Handle);
        _constraintHandles[constraint] = handle;
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
        var spans = new List<ConstraintSpan>();
        foreach (ConstraintPathHandle path in handle.Paths)
        {
            if (path.Points.Count < 2) return false;
            Node[] nodes = path.Points.Select(point => LiveNode(point.NodeId)).OfType<Node>().ToArray();
            if (nodes.Length != path.Points.Count) return false;
            spans.AddRange(nodes.Zip(nodes.Skip(1), (from, to) => new ConstraintSpan(from, to)));
        }

        ConstraintPoint[] points = handle.Points.Select(point =>
        {
            Node? node = LiveNode(point.Node.NodeId);
            return node is null ? null : new ConstraintPoint(node, point.Name);
        }).OfType<ConstraintPoint>().ToArray();
        if (points.Length != handle.Points.Count) return false;

        var constraint = new Constraint(points: points, spans: spans, name: handle.Name);
        if (!_mesher.TryInsertConstraint(constraint, out _)) return false;
        if (handle.Current is not null) _constraintHandles.Remove(handle.Current);
        handle.Current = constraint;
        _constraintHandles[constraint] = handle;
        return true;
    }

    internal bool RemoveConstraintHandle(ConstraintHandle handle)
    {
        if (handle.Current is null) return false;
        return _mesher.TryRemoveConstraint(handle.Current, out _);
    }

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
