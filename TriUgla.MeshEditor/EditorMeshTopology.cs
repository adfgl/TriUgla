namespace TriUgla.MeshEditor;

public sealed partial class EditorMeshModel
{
    const double CommandPositionTolerance = 1e-9;

    int Id(Node node)
    {
        if (_ids.TryGetValue(node, out int id)) return id;
        id = _nextId++;
        _ids.Add(node, id);
        return id;
    }

    Node? LiveNode(int id)
        => _ids.FirstOrDefault(pair => pair.Value == id && !pair.Key.Dead).Key;

    Node? LiveNode(Vec2 position)
    {
        Node[] nodes = _mesher.Traversal.Nodes().Where(node => !node.Dead).ToArray();
        Node? exact = nodes.FirstOrDefault(node => node.Position == position);
        if (exact is not null) return exact;
        double toleranceSquared = CommandPositionTolerance * CommandPositionTolerance;
        return nodes
            .Select(node => (Node: node, Distance: node.Position.DistanceSquared(position)))
            .Where(candidate => candidate.Distance <= toleranceSquared)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Node)
            .FirstOrDefault();
    }

    internal int? NodeIdAt(Vec2 position)
        => LiveNode(position) is Node node ? Id(node) : null;

    internal bool SetNodeData(Vec2 position, NodeData data)
    {
        Node? node = LiveNode(position);
        if (node is null)
        {
            Fail($"No live node exists at {position}.");
            return false;
        }
        node.Data = data;
        return true;
    }

    internal bool InsertPoint(Vec2 position, int? loopIndex)
    {
        if (loopIndex is not null && !ReleaseInitialLoop()) return false;
        InsertNodeResult result = _mesher.Insert(position);
        bool inserted = result.Status is InsertNodeStatus.InsertedIntoFace or InsertNodeStatus.InsertedIntoEdge;
        if (!inserted || result.Node is null) return false;
        Id(result.Node);
        if (loopIndex is int index)
        {
            _loopPoints.Insert(Math.Min(index, _loopPoints.Count), position);
            return RebuildInitialLoop();
        }
        return true;
    }

    internal bool RemovePoint(Vec2 position, int? loopIndex)
    {
        Node? node = LiveNode(position);
        if (node is null)
        {
            Fail($"No live node exists at {position}.");
            return false;
        }
        if (loopIndex is not null && !ReleaseInitialLoop()) return false;
        bool removed = _mesher.Remove(position).Removed;
        if (!removed)
            Fail(node.Constrained
                ? $"Node at {position} cannot be removed because ConstraintCount is {node.ConstraintCount}."
                : $"Node at {position} could not be removed from the current topology.");
        if (removed && loopIndex is not null)
        {
            _loopPoints.Remove(position);
            return RebuildInitialLoop();
        }
        return removed;
    }

    internal bool InsertConstraintHandle(ConstraintHandle handle)
    {
        var lines = new List<ConstraintLine>();
        foreach (ConstraintPathHandle path in handle.Paths)
        {
            if (path.Points.Count < 2) { Fail("A constraint path needs at least two points."); return false; }
            if (path.Points.Zip(path.Points.Skip(1)).Any(pair => pair.First == pair.Second))
            { Fail("A constraint path contains two identical consecutive positions."); return false; }
            Node[] nodes = path.Points.Select(LiveNode).OfType<Node>().ToArray();
            if (nodes.Length != path.Points.Count)
            { Fail("A constraint endpoint has no matching live node."); return false; }
            if (nodes.Zip(nodes.Skip(1)).Any(pair => ReferenceEquals(pair.First, pair.Second)))
            { Fail("Two constraint endpoints resolved to the same live node."); return false; }
            lines.AddRange(nodes.Zip(nodes.Skip(1), (from, to) => new ConstraintLine(from, to)));
        }

        ConstraintPoint[] points = handle.Points.Select(point =>
        {
            Node? node = LiveNode(point.Position);
            return node is null ? null : new ConstraintPoint(node, point.Name);
        }).OfType<ConstraintPoint>().ToArray();
        if (points.Length != handle.Points.Count)
        { Fail("A constrained point has no matching live node."); return false; }

        var insertedLines = new List<ConstraintLine>();
        var insertedPoints = new List<ConstraintPoint>();
        foreach (ConstraintLine line in lines)
        {
            if (_mesher.TryInsertConstraint(line, out string? reason))
            { insertedLines.Add(line); continue; }
            foreach (ConstraintLine inserted in insertedLines.AsEnumerable().Reverse())
                _mesher.TryRemoveConstraint(inserted, out _);
            Fail(reason);
            return false;
        }
        foreach (ConstraintPoint point in points)
        {
            if (_mesher.TryInsertConstraint(point, out string? reason))
            { insertedPoints.Add(point); continue; }
            foreach (ConstraintPoint inserted in insertedPoints.AsEnumerable().Reverse())
                _mesher.TryRemoveConstraint(inserted, out _);
            foreach (ConstraintLine inserted in insertedLines.AsEnumerable().Reverse())
                _mesher.TryRemoveConstraint(inserted, out _);
            Fail(reason);
            return false;
        }
        var constraint = new EditorConstraint(handle.Name, points, lines);
        _constraints.Add(constraint);
        _constraintDefinitions[constraint] = handle;
        return true;
    }

    internal bool RemoveConstraintHandle(ConstraintHandle handle)
    {
        EditorConstraint? constraint = _constraints.FirstOrDefault(candidate => Matches(candidate, handle));
        if (constraint is null)
        {
            Fail($"Constraint '{handle.Name}' is not present in the current mesh.");
            return false;
        }
        foreach (ConstraintLine line in constraint.Lines.AsEnumerable().Reverse())
        {
            if (_mesher.TryRemoveConstraint(line, out string? reason)) continue;
            Fail(reason);
            return false;
        }
        foreach (ConstraintPoint point in constraint.Points.AsEnumerable().Reverse())
        {
            if (_mesher.TryRemoveConstraint(point, out string? reason)) continue;
            Fail(reason);
            return false;
        }
        _constraints.Remove(constraint);
        _constraintDefinitions.Remove(constraint);
        return true;
    }

    internal bool InsertLoopHandle(LoopHandle handle)
    {
        Node[] nodes = handle.Points.Select(LiveNode).OfType<Node>().ToArray();
        if (nodes.Length != handle.Points.Count) return false;
        var loop = new Loop(nodes, handle.Name);
        if (!_mesher.TryInsertLoop(loop, out _)) return false;
        try { Classify(); }
        catch (InvalidOperationException)
        {
            _mesher.TryRemoveLoop(loop, out _);
            return false;
        }
        return true;
    }

    internal bool RemoveLoopHandle(LoopHandle handle)
    {
        Node[] nodes = handle.Points.Select(LiveNode).OfType<Node>().ToArray();
        if (nodes.Length != handle.Points.Count)
        {
            Fail("A polygon point has no matching live node.");
            return false;
        }
        if (_mesher.TryRemoveLoop(new Loop(nodes, handle.Name), out string? reason)) return true;
        Fail(reason);
        return false;
    }

    static bool Matches(EditorConstraint constraint, ConstraintHandle handle)
        => constraint.Lines.Select(line => (line.From.Position, line.To.Position))
               .SequenceEqual(handle.Paths.SelectMany(path => path.Points.Zip(path.Points.Skip(1),
                   (from, to) => (from, to)))) &&
           constraint.Points.Select(point => (point.Node.Position, point.Name))
               .SequenceEqual(handle.Points.Select(point => (point.Position, point.Name)));

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
        Node[] nodes = _loopPoints.Select(position => LiveNode(position)!).ToArray();
        var loop = new Loop(nodes, "Initial rectangle");
        if (!_mesher.TryInsertLoop(loop, out _)) return false;
        _initialLoop = loop;
        return true;
    }
}
