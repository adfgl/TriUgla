namespace TriUgla;

public sealed class Mesher
{
    readonly Mesh _mesh;
    readonly MeshLocator _locator;
    readonly NodeInserter _nodeInserter;
    readonly NodeRemover _nodeRemover;
    readonly EdgeInserter _edgeInserter;
    readonly EdgeLegalizer _edgeLegalizer;
    readonly MeshRefiner _refiner;
    readonly MeshDegrader _degrader;
    readonly GeometryPredicates _geometry;
    readonly SuperStructure? _superStructure;
    readonly Constraints _constraints = new();

    public Mesher(Vec2 min, Vec2 max, int superStructureSideCount = 3)
        : this(SuperStructure.Make(min, max, superStructureSideCount))
    {
    }

    public Mesher(SuperStructure superStructure)
        : this(new Mesh((superStructure ?? throw new ArgumentNullException(nameof(superStructure))).Root))
        => _superStructure = superStructure;

    public Mesher(Face root) : this(new Mesh(root))
    {
    }

    public Mesher(Mesh mesh)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        _locator = new MeshLocator(mesh);
        var splitter = new Splitter();
        _geometry = new GeometryPredicates();
        var flipper = new EdgeFlipper(_geometry);
        _nodeInserter = new NodeInserter(new NodeFactory(), splitter, _locator);
        _nodeRemover = new NodeRemover(_geometry);
        _edgeInserter = new EdgeInserter(_geometry, flipper, splitter, new NodeFactory());
        _edgeLegalizer = new EdgeLegalizer(flipper);
        _refiner = new MeshRefiner(
            _geometry,
            _locator,
            _edgeLegalizer,
            splitter,
            _nodeInserter);
        _degrader = new MeshDegrader(this);
    }

    public Mesh Mesh => _mesh;
    public Face Root => _mesh.Root;
    public GeometryPredicates Geometry => _geometry;
    public SuperStructure? SuperStructure => _superStructure;
    public IConstraints Constraints => _constraints;

    public LocateResult Locate(Vec2 point, Face? from = null)
        => _locator.Locate(point, from);

    public MeshElement? Find(Vec2 point, Face? from = null)
    {
        LocateResult result = Locate(point, from);
        return result.Node ?? (MeshElement?)result.Edge ?? result.Face;
    }

    public InsertNodeResult Insert(Vec2 position, Face? from = null)
    {
        InsertNodeResult result = _nodeInserter.Insert(position, from);
        TopologyChange? change = result.FaceSplit?.Change ?? result.EdgeSplit?.Change;
        if (change is not null)
        {
            TopologyChanged(change.Value.AffectedFaces);
            Legalize(change.Value.EdgesToLegalize);
        }
        return result;
    }

    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        RemoveNodeResult result = _nodeRemover.Remove(
            node, out NodeRemover.ConstrainedRemoval? constrained);
        if (!result.Removed) return result;
        TopologyChanged(result.Change.AffectedFaces);
        Legalize(result.Change.EdgesToLegalize);
        if (constrained is NodeRemover.ConstrainedRemoval dissolved)
        {
            RestoreSegment(
                dissolved.First, dissolved.Second,
                dissolved.Forward, dissolved.Reverse);
        }
        return result;
    }

    void RestoreSegment(
        Node first,
        Node second,
        NodeRemover.ConstraintCounts forward,
        NodeRemover.ConstraintCounts reverse)
    {
        for (int count = 0; count < forward.Features; count++)
            InsertEdge(first, second, EdgeConstraintKind.Feature);
        for (int count = 0; count < forward.Boundaries; count++)
            InsertEdge(first, second, EdgeConstraintKind.Boundary);
        for (int count = 0; count < reverse.Features; count++)
            InsertEdge(second, first, EdgeConstraintKind.Feature);
        for (int count = 0; count < reverse.Boundaries; count++)
            InsertEdge(second, first, EdgeConstraintKind.Boundary);
    }

    public RemoveNodeResult Remove(Vec2 position)
    {
        Node? node = _locator.Locate(position).Node ?? throw new InvalidOperationException(
                $"Cannot remove node at {position}: no node exists at that position.");
        return Remove(node);
    }

    public int Refine(FaceRanker ranker, in RefineSettings settings)
        => Refine(_mesh.Faces(), ranker, in settings, CancellationToken.None);

    public int Refine(
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
        => Refine(_mesh.Faces(), ranker, in settings, cancellationToken);

    public int Refine(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings)
        => Refine(faces, ranker, in settings, CancellationToken.None);

    public int Refine(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
        => RefineDetailed(faces, ranker, in settings, cancellationToken).InsertedNodes;

    public RefineResult RefineDetailed(
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken = default)
        => RefineDetailed(_mesh.Faces(), ranker, in settings, cancellationToken);

    public RefineResult RefineDetailed(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(faces);
        ArgumentNullException.ThrowIfNull(ranker);

        ClassifyFaces();
        Face[] selected = faces.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        RefineResult result = _refiner.RefineDetailed(
            selected, ranker, in settings, cancellationToken);
        SynchronizeTopology();
        return result;
    }

    public int Degrade(IEnumerable<Node> candidates)
        => _degrader.Degrade(candidates);

    public bool TryInsertConstraint(ConstraintPoint point, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(point);
        if (!ValidateNode(point.Node, out string why))
            return Fail(out reason, $"Constraint point '{point.Name}': {why}");

        try
        {
            point.Node.Constrain();
            _constraints.Points.Add(point);
            reason = null;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            reason = $"Constraint point '{point.Name}' insertion failed: {exception.Message}";
            return false;
        }
    }

    public bool TryInsertConstraint(ConstraintLine line, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (!ValidateConstraint(line, out reason)) return false;

        try
        {
            InsertEdge(line.From, line.To, EdgeConstraintKind.Feature);
            AssertConstrainedPath(line.From, line.To, EdgeConstraintKind.Feature);
            _constraints.Lines.Add(line);
            reason = null;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            reason = $"Constraint line '{line.Name}' insertion failed: {exception.Message}";
            return false;
        }
    }

    public bool TryRemoveConstraint(ConstraintPoint point, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(point);
        ConstraintPoint? registered = FindConstraintPoint(point);
        if (registered is null)
            return Fail(out reason, $"Constraint point '{point.Name}' not found in mesh.");
        if (!registered.Node.Constrained)
            return Fail(out reason, $"Constraint point '{point.Name}': node is not constrained.");

        registered.Node.Relax();
        _constraints.Points.Remove(registered);
        reason = null;
        return true;
    }

    public bool TryRemoveConstraint(ConstraintLine line, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(line);
        ConstraintLine? registered = FindConstraintLine(line);
        if (registered is null)
            return Fail(out reason, $"Constraint line '{line.Name}' not found in mesh.");

        if (!TryResolvePath(registered, out List<Edge> path, out string? pathReason))
            return Fail(out reason, $"Constraint line '{line.Name}': {pathReason}");
        if (path.Count == 0)
            return Fail(out reason, $"Constraint line '{line.Name}': produced no edges.");
        if (path.Any(edge => !edge.HasFeature))
            return Fail(out reason, $"Constraint line '{line.Name}': edge in path has no Feature constraint.");

        HashSet<Node> steinerNodes = CollectSteinerInsertions(
            [path], [registered.From, registered.To]);
        ReleasePaths([path], EdgeConstraintKind.Feature);
        _constraints.Lines.Remove(registered);
        RemoveReleasedSteinerInsertions(steinerNodes);
        reason = null;
        return true;
    }

    public bool TryInsertPolyline(Polyline polyline, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        if (polyline.Nodes.Count < 2)
            return Fail(out reason, $"{PolylineContext(polyline)} invalid: must have at least 2 points.");
        for (int i = 0; i < polyline.Nodes.Count; i++)
        {
            if (!ValidateNode(polyline.Nodes[i], out string why))
                return Fail(out reason, $"{PolylineContext(polyline)} invalid: node[{i}] {why}");
            if (i > 0 && (ReferenceEquals(polyline.Nodes[i - 1], polyline.Nodes[i]) ||
                          polyline.Nodes[i - 1].Position == polyline.Nodes[i].Position))
                return Fail(out reason, $"{PolylineContext(polyline)} invalid: segment[{i - 1}] endpoints must be distinct.");
        }

        try
        {
            for (int i = 0; i < polyline.Nodes.Count - 1; i++)
                InsertEdge(polyline.Nodes[i], polyline.Nodes[i + 1], EdgeConstraintKind.Feature);
            _constraints.Polylines.Add(polyline);
            reason = null;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            reason = $"{PolylineContext(polyline)} insertion failed: {exception.Message}";
            return false;
        }
    }

    public bool TryRemovePolyline(Polyline polyline, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        Polyline? registered = FindPolyline(polyline);
        if (registered is null)
            return Fail(out reason, $"{PolylineContext(polyline)} not found in mesh.");

        var paths = new List<List<Edge>>(registered.Nodes.Count - 1);
        for (int i = 0; i < registered.Nodes.Count - 1; i++)
        {
            var line = new ConstraintLine(registered.Nodes[i], registered.Nodes[i + 1]);
            if (!TryResolvePath(line, out List<Edge> path, out string? pathReason))
                return Fail(out reason, $"{PolylineContext(polyline)} segment[{i}]: {pathReason}");
            if (path.Count == 0 || path.Any(edge => !edge.HasFeature))
                return Fail(out reason, $"{PolylineContext(polyline)} segment[{i}] has no Feature constraint.");
            paths.Add(path);
        }

        HashSet<Node> steinerNodes = CollectSteinerInsertions(paths, registered.Nodes);
        ReleasePaths(paths, EdgeConstraintKind.Feature);
        _constraints.Polylines.Remove(registered);
        RemoveReleasedSteinerInsertions(steinerNodes);
        reason = null;
        return true;
    }

    public bool TryInsertLoop(Loop loop, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(loop);
        loop.Close();
        if (loop.Nodes.Count < 4)
            return Fail(out reason, $"{LoopContext(loop)} invalid: must have at least 3 unique points.");
        if (loop.SignedArea() == 0d)
            return Fail(out reason, $"{LoopContext(loop)} invalid: zero area.");
        if (SelfIntersects(loop))
            return Fail(out reason, $"{LoopContext(loop)} invalid: self-intersecting.");

        for (int i = 0; i < loop.Nodes.Count - 1; i++)
        {
            if (!ValidateNode(loop.Nodes[i], out string why))
                return Fail(out reason, $"{LoopContext(loop)} invalid: node[{i}] {why}");
        }

        try
        {
            for (int i = 0; i < loop.Nodes.Count - 1; i++)
                InsertEdge(loop.Nodes[i], loop.Nodes[i + 1], EdgeConstraintKind.Boundary);

            _constraints.Loops.Add(loop);
            reason = null;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            reason = $"{LoopContext(loop)} insertion failed: {exception.Message}";
            return false;
        }
    }

    public bool TryRemoveLoop(Loop loop, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(loop);
        Loop? registered = FindLoop(loop);
        if (registered is null)
            return Fail(out reason, $"{LoopContext(loop)} not found in mesh.");

        registered.Close();
        var paths = new List<List<Edge>>(registered.Nodes.Count - 1);
        for (int i = 0; i < registered.Nodes.Count - 1; i++)
        {
            var line = new ConstraintLine(registered.Nodes[i], registered.Nodes[i + 1]);
            if (!TryResolvePath(line, out List<Edge> path, out string? pathReason))
                return Fail(out reason, $"{LoopContext(loop)} edge[{i}]: {pathReason}");
            if (path.Count == 0 || path.Any(edge => !edge.HasBoundary))
                return Fail(out reason, $"{LoopContext(loop)} edge[{i}] has no Boundary constraint.");
            paths.Add(path);
        }

        HashSet<Node> steinerNodes = CollectSteinerInsertions(paths, registered.Nodes);
        ReleasePaths(paths, EdgeConstraintKind.Boundary);
        _constraints.Loops.Remove(registered);
        RemoveReleasedSteinerInsertions(steinerNodes);
        reason = null;
        return true;
    }

    ConstraintPoint? FindConstraintPoint(ConstraintPoint requested)
        => _constraints.Points.FirstOrDefault(candidate => ReferenceEquals(candidate, requested)) ??
           _constraints.Points.FirstOrDefault(candidate =>
               candidate.Node.Position == requested.Node.Position);

    ConstraintLine? FindConstraintLine(ConstraintLine requested)
        => _constraints.Lines.FirstOrDefault(candidate => ReferenceEquals(candidate, requested)) ??
           _constraints.Lines.FirstOrDefault(candidate =>
               SameLine(candidate, requested));

    Polyline? FindPolyline(Polyline requested)
        => _constraints.Polylines.FirstOrDefault(candidate => ReferenceEquals(candidate, requested)) ??
           _constraints.Polylines.FirstOrDefault(candidate =>
               SamePath(candidate.Nodes, requested.Nodes, allowReverse: true));

    Loop? FindLoop(Loop requested)
    {
        Loop? exact = _constraints.Loops.FirstOrDefault(candidate => ReferenceEquals(candidate, requested));
        if (exact is not null) return exact;
        Vec2[] requestedPoints = OpenLoopPositions(requested);
        return _constraints.Loops.FirstOrDefault(candidate =>
            SameCycle(OpenLoopPositions(candidate), requestedPoints));
    }

    static bool SameLine(ConstraintLine left, ConstraintLine right)
        => left.From.Position == right.From.Position && left.To.Position == right.To.Position ||
           left.From.Position == right.To.Position && left.To.Position == right.From.Position;

    static bool SamePath(IReadOnlyList<Node> left, IReadOnlyList<Node> right, bool allowReverse)
    {
        if (left.Count != right.Count) return false;
        bool forward = left.Select(node => node.Position)
            .SequenceEqual(right.Select(node => node.Position));
        return forward || allowReverse && left.Select(node => node.Position)
            .SequenceEqual(right.Reverse().Select(node => node.Position));
    }

    static Vec2[] OpenLoopPositions(Loop loop)
    {
        int count = loop.Nodes.Count;
        if (count > 1 && loop.Nodes[0].Position == loop.Nodes[^1].Position) count--;
        return loop.Nodes.Take(count).Select(node => node.Position).ToArray();
    }

    static bool SameCycle(IReadOnlyList<Vec2> left, IReadOnlyList<Vec2> right)
    {
        if (left.Count != right.Count) return false;
        if (left.Count == 0) return true;
        for (int start = 0; start < right.Count; start++)
        {
            if (left[0] != right[start]) continue;
            bool forward = true;
            bool reverse = true;
            for (int offset = 1; offset < left.Count && (forward || reverse); offset++)
            {
                forward &= left[offset] == right[(start + offset) % right.Count];
                reverse &= left[offset] == right[(start - offset + right.Count) % right.Count];
            }
            if (forward || reverse) return true;
        }
        return false;
    }

    void InsertEdge(Node start, Node end, EdgeConstraintKind kind)
    {
        EdgeInsertResult result = _edgeInserter.Insert(start, end, kind);
        if (result.ConstrainedEdges.Count == 0 ||
            result.ConstrainedEdges.Any(edge => !HasConstraint(edge, kind)))
        {
            throw new InvalidOperationException(
                $"Edge insertion from {start.Position} to {end.Position} did not mark every " +
                $"inserted segment as {kind} constrained.");
        }
        TopologyChanged(result.Change.AffectedFaces);
        Legalize(result.Change.EdgesToLegalize);
        AssertConstrainedPath(start, end, kind);
    }

    static bool HasConstraint(Edge edge, EdgeConstraintKind kind)
        => kind switch
        {
            EdgeConstraintKind.Feature => edge.HasFeature,
            EdgeConstraintKind.Boundary => edge.HasBoundary,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    static void AssertConstrainedPath(Node start, Node end, EdgeConstraintKind kind)
    {
        var path = new ConstraintLine(start, end).Edges([]);
        if (path.Count == 0 || path.Any(edge => !HasConstraint(edge, kind)))
        {
            throw new InvalidOperationException(
                $"Edge insertion from {start.Position} to {end.Position} produced a path " +
                $"with one or more segments not marked as {kind} constrained.");
        }
    }

    void ReleasePaths(IEnumerable<List<Edge>> paths, EdgeConstraintKind kind)
    {
        var candidates = new Queue<Edge>();
        foreach (Edge edge in paths.SelectMany(path => path))
        {
            edge.Release(kind);
            candidates.Enqueue(edge);
        }
        Legalize(candidates);
    }

    static HashSet<Node> CollectSteinerInsertions(
        IEnumerable<List<Edge>> paths,
        IEnumerable<Node> protectedNodes)
    {
        var protectedSet = new HashSet<Node>(protectedNodes, ReferenceEqualityComparer.Instance);
        var result = new HashSet<Node>(ReferenceEqualityComparer.Instance);
        foreach (Edge edge in paths.SelectMany(path => path))
        {
            if (edge.NodeStart.Kind == NodeKind.SteinerInsertion &&
                !protectedSet.Contains(edge.NodeStart)) result.Add(edge.NodeStart);
            if (edge.NodeEnd.Kind == NodeKind.SteinerInsertion &&
                !protectedSet.Contains(edge.NodeEnd)) result.Add(edge.NodeEnd);
        }
        return result;
    }

    void RemoveReleasedSteinerInsertions(IEnumerable<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            if (node.PromotedForInsertion && !node.Constrained)
            {
                node.ReleaseInsertionRole();
                continue;
            }
            if (!ShouldRemoveInsertionSteiner(node)) continue;
            if (!node.Constrained) node.ReleaseInsertionRole();
            if (!Remove(node).Removed && !node.Constrained) node.ReleaseInsertionRole();
        }
    }

    bool ShouldRemoveInsertionSteiner(Node node)
        => node.Kind == NodeKind.SteinerInsertion &&
           !node.Dead;

    void Legalize(IEnumerable<Edge> candidates)
    {
        var queue = candidates as Queue<Edge> ?? new Queue<Edge>(candidates);
        if (queue.Count == 0) return;
        EdgeLegalizationResult result = _edgeLegalizer.Legalize(queue);
        TopologyChanged(result.AffectedFaces);
    }

    bool ValidateConstraint(ConstraintLine line, out string? reason)
    {
        if (!ValidateNode(line.From, out string fromWhy))
            return Fail(out reason, $"Constraint line '{line.Name}' From: {fromWhy}");
        if (!ValidateNode(line.To, out string toWhy))
            return Fail(out reason, $"Constraint line '{line.Name}' To: {toWhy}");
        if (ReferenceEquals(line.From, line.To) || line.From.Position == line.To.Position)
            return Fail(out reason, $"Constraint line '{line.Name}': endpoints must be distinct.");
        reason = null;
        return true;
    }

    bool ValidateNode(Node node, out string why)
    {
        if (node.Dead) { why = "node is invalid."; return false; }
        if (_superStructure?.SuperNode(node) == true)
        { why = "node is part of the super structure."; return false; }
        if (!ContainsNode(node))
        { why = "node does not belong to this mesh."; return false; }
        why = string.Empty;
        return true;
    }

    bool ContainsNode(Node node)
    {
        if (node.Dead) return false;
        LocateResult location = _locator.Locate(node.Position);
        return ReferenceEquals(location.Node, node);
    }

    bool SelfIntersects(Loop loop)
    {
        int edgeCount = loop.Nodes.Count - 1;
        for (int i = 0; i < edgeCount; i++)
        {
            Vec2 a = loop.Nodes[i].Position;
            Vec2 b = loop.Nodes[i + 1].Position;
            if (a == b) return true;
            for (int j = i + 1; j < edgeCount; j++)
            {
                if (j == i + 1 || i == 0 && j == edgeCount - 1) continue;
                Vec2 c = loop.Nodes[j].Position;
                Vec2 d = loop.Nodes[j + 1].Position;
                if (c == d || _geometry.Intersects(a, b, c, d) >= 0) return true;
            }
        }
        return false;
    }

    static bool TryResolvePath(ConstraintLine line, out List<Edge> path, out string? reason)
    {
        path = [];
        try { line.Edges(path); reason = null; return true; }
        catch (InvalidOperationException exception) { reason = exception.Message; return false; }
    }

    static bool Fail(out string? reason, string message) { reason = message; return false; }
    static string PolylineContext(Polyline polyline) => $"Polyline '{polyline.Name}'";
    static string LoopContext(Loop loop) => $"Loop '{loop.Name}'";

    void TopologyChanged(IReadOnlyList<Face> affectedFaces)
    {
        if (_mesh.RootDead)
        {
            Face replacement = affectedFaces.FirstOrDefault(face => !face.Dead)
                ?? throw new InvalidOperationException(
                    "A topology change retired the root without a replacement face.");
            _mesh.SetRoot(replacement);
        }
        _locator.Reset();
    }

    void ClassifyFaces()
    {
        if (_superStructure is null)
        {
            throw new InvalidOperationException(
                "Refinement requires a Mesher created with a SuperStructure or bounds " +
                "so faces can be classified first.");
        }

        new FaceClassifier(_mesh, _superStructure).Classify();
    }

    void SynchronizeTopology()
    {
        if (_mesh.RootDead)
        {
            _ = _mesh.Root;
        }
        _locator.Reset();
    }
}
