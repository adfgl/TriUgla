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
    readonly GeometryPredicates _geometry;
    readonly SuperStructure? _superStructure;

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
        Face root = mesh.Root;
        var stamps = new StampSource();
        Traversal = new MeshTraversal(root, stamps);
        _locator = new MeshLocator(mesh, Traversal, stamps);
        var splitter = new Splitter();
        _geometry = new GeometryPredicates();
        var flipper = new EdgeFlipper(_geometry);
        _nodeInserter = new NodeInserter(new NodeFactory(), splitter, _locator);
        _nodeRemover = new NodeRemover();
        _edgeInserter = new EdgeInserter(_geometry, flipper, splitter, new NodeFactory());
        _edgeLegalizer = new EdgeLegalizer(flipper);
        _refiner = new MeshRefiner(
            _geometry,
            _locator,
            _edgeLegalizer,
            splitter,
            _nodeInserter,
            Traversal);
    }

    public Mesh Mesh => _mesh;
    public Face Root => _mesh.Root;
    public MeshTraversal Traversal { get; }
    public GeometryPredicates Geometry => _geometry;
    public SuperStructure? SuperStructure => _superStructure;
    public Constraints Constraints { get; } = new();

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
        RemoveNodeResult result = _nodeRemover.Remove(node);
        if (result.Removed)
        {
            TopologyChanged(result.Change.AffectedFaces);
            Legalize(result.Change.EdgesToLegalize);
        }
        return result;
    }

    public RemoveNodeResult Remove(Vec2 position)
    {
        Node? node = _locator.Locate(position).Node ?? throw new InvalidOperationException(
                $"Cannot remove node at {position}: no node exists at that position.");
        return Remove(node);
    }

    public int Refine(FaceRanker ranker, in RefineSettings settings)
        => Refine(Traversal.Faces(), ranker, in settings, CancellationToken.None);

    public int Refine(
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
        => Refine(Traversal.Faces(), ranker, in settings, cancellationToken);

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
    {
        ArgumentNullException.ThrowIfNull(faces);
        ArgumentNullException.ThrowIfNull(ranker);

        ClassifyFaces();
        Face[] selected = faces.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        int inserted = _refiner.Refine(selected, ranker, in settings, cancellationToken);
        SynchronizeTopology();
        return inserted;
    }

    public async ValueTask<int> RefineAsync(
        FaceRanker ranker,
        RefineSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ranker);
        ClassifyFaces();
        Face[] selected = Traversal.Faces().ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        int inserted = await _refiner.RefineAsync(
            selected,
            ranker,
            settings,
            cancellationToken);
        SynchronizeTopology();
        return inserted;
    }

    public bool TryInsertConstraint(ConstraintPoint point, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(point);
        if (!ValidateNode(point.Node, out string why))
            return Fail(out reason, $"Constraint point '{point.Name}': {why}");

        try
        {
            point.Node.Constrain();
            Constraints.Points.Add(point);
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
            Constraints.Lines.Add(line);
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
        if (!Constraints.Points.Contains(point))
            return Fail(out reason, $"Constraint point '{point.Name}' not found in mesh.");
        if (!point.Node.Constrained)
            return Fail(out reason, $"Constraint point '{point.Name}': node is not constrained.");

        point.Node.Relax();
        Constraints.Points.Remove(point);
        reason = null;
        return true;
    }

    public bool TryRemoveConstraint(ConstraintLine line, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (!Constraints.Lines.Contains(line))
            return Fail(out reason, $"Constraint line '{line.Name}' not found in mesh.");

        if (!TryResolvePath(line, out List<Edge> path, out string? pathReason))
            return Fail(out reason, $"Constraint line '{line.Name}': {pathReason}");
        if (path.Count == 0)
            return Fail(out reason, $"Constraint line '{line.Name}': produced no edges.");
        if (path.Any(edge => !edge.HasFeature))
            return Fail(out reason, $"Constraint line '{line.Name}': edge in path has no Feature constraint.");

        HashSet<Node> steinerNodes = CollectSteinerInsertions([path], [line.From, line.To]);
        ReleasePaths([path], EdgeConstraintKind.Feature);
        Constraints.Lines.Remove(line);
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
            Constraints.Polylines.Add(polyline);
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
        int index = Constraints.Polylines.IndexOf(polyline);
        if (index < 0) return Fail(out reason, $"{PolylineContext(polyline)} not found in mesh.");

        var paths = new List<List<Edge>>(polyline.Nodes.Count - 1);
        for (int i = 0; i < polyline.Nodes.Count - 1; i++)
        {
            var line = new ConstraintLine(polyline.Nodes[i], polyline.Nodes[i + 1]);
            if (!TryResolvePath(line, out List<Edge> path, out string? pathReason))
                return Fail(out reason, $"{PolylineContext(polyline)} segment[{i}]: {pathReason}");
            if (path.Count == 0 || path.Any(edge => !edge.HasFeature))
                return Fail(out reason, $"{PolylineContext(polyline)} segment[{i}] has no Feature constraint.");
            paths.Add(path);
        }

        HashSet<Node> steinerNodes = CollectSteinerInsertions(paths, polyline.Nodes);
        ReleasePaths(paths, EdgeConstraintKind.Feature);
        Constraints.Polylines.RemoveAt(index);
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

            Constraints.Loops.Add(loop);
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
        int index = Constraints.Loops.IndexOf(loop);
        if (index < 0) return Fail(out reason, $"{LoopContext(loop)} not found in mesh.");

        loop.Close();
        var paths = new List<List<Edge>>(loop.Nodes.Count - 1);
        for (int i = 0; i < loop.Nodes.Count - 1; i++)
        {
            var line = new ConstraintLine(loop.Nodes[i], loop.Nodes[i + 1]);
            if (!TryResolvePath(line, out List<Edge> path, out string? pathReason))
                return Fail(out reason, $"{LoopContext(loop)} edge[{i}]: {pathReason}");
            if (path.Count == 0 || path.Any(edge => !edge.HasBoundary))
                return Fail(out reason, $"{LoopContext(loop)} edge[{i}] has no Boundary constraint.");
            paths.Add(path);
        }

        HashSet<Node> steinerNodes = CollectSteinerInsertions(paths, loop.Nodes);
        ReleasePaths(paths, EdgeConstraintKind.Boundary);
        Constraints.Loops.RemoveAt(index);
        RemoveReleasedSteinerInsertions(steinerNodes);
        reason = null;
        return true;
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
            if (node.Dead || IsStructuralAnchor(node)) continue;
            ConstrainedSpoke[] spokes = ConstrainedSpokes(node);
            if (spokes.Length == 0)
            {
                Remove(node);
                continue;
            }
            if (spokes.Length == 1)
            {
                Release(spokes[0]);
                Remove(node);
                continue;
            }
            if (spokes.Length != 2 || !CanDissolve(node, spokes[0], spokes[1])) continue;

            Node first = spokes[0].Other;
            Node second = spokes[1].Other;
            ConstraintCounts forward = Counts(spokes[0], first, node);
            ConstraintCounts reverse = Counts(spokes[0], node, first);
            Release(spokes[0]);
            Release(spokes[1]);
            if (!Remove(node).Removed)
                throw new InvalidOperationException(
                    $"Could not dissolve non-structural Steiner node at {node.Position}.");
            for (int count = 0; count < forward.Features; count++)
                InsertEdge(first, second, EdgeConstraintKind.Feature);
            for (int count = 0; count < forward.Boundaries; count++)
                InsertEdge(first, second, EdgeConstraintKind.Boundary);
            for (int count = 0; count < reverse.Features; count++)
                InsertEdge(second, first, EdgeConstraintKind.Feature);
            for (int count = 0; count < reverse.Boundaries; count++)
                InsertEdge(second, first, EdgeConstraintKind.Boundary);
        }
    }

    bool IsStructuralAnchor(Node node)
        => Constraints.Points.Any(point => ReferenceEquals(point.Node, node)) ||
           Constraints.Lines.Any(line =>
               ReferenceEquals(line.From, node) || ReferenceEquals(line.To, node)) ||
           Constraints.Polylines.Any(polyline => polyline.Nodes.Any(candidate => ReferenceEquals(candidate, node))) ||
           Constraints.Loops.Any(loop => loop.Nodes.Any(candidate => ReferenceEquals(candidate, node)));

    bool CanDissolve(Node node, ConstrainedSpoke first, ConstrainedSpoke second)
        => Counts(first, first.Other, node) == Counts(second, node, second.Other) &&
           Counts(first, node, first.Other) == Counts(second, second.Other, node) &&
           _geometry.Orient(first.Other, second.Other, node.Position) == EOrientaiton.Collinear &&
           (first.Other.Position - node.Position).Dot(second.Other.Position - node.Position) < 0d;

    static ConstraintCounts Counts(ConstrainedSpoke spoke, Node from, Node to)
    {
        Edge[] directed = spoke.HalfEdges.Where(edge =>
            ReferenceEquals(edge.NodeStart, from) && ReferenceEquals(edge.NodeEnd, to)).ToArray();
        return new ConstraintCounts(
            directed.Sum(edge => edge.FeatureConstraints),
            directed.Sum(edge => edge.BoundaryConstraints));
    }

    ConstrainedSpoke[] ConstrainedSpokes(Node node)
    {
        var groups = new Dictionary<Node, List<Edge>>(ReferenceEqualityComparer.Instance);
        foreach (Edge edge in Traversal.Edges())
        {
            if (edge.Dead || !edge.Contains(node) || !edge.Constrained) continue;
            Node other = ReferenceEquals(edge.NodeStart, node) ? edge.NodeEnd : edge.NodeStart;
            if (!groups.TryGetValue(other, out List<Edge>? halfEdges))
                groups.Add(other, halfEdges = []);
            halfEdges.Add(edge);
        }
        return groups.Select(group => new ConstrainedSpoke(
            group.Key,
            group.Value.ToArray())).ToArray();
    }

    static void Release(ConstrainedSpoke spoke)
    {
        foreach (Edge edge in spoke.HalfEdges)
        {
            while (edge.HasFeature) edge.Release(EdgeConstraintKind.Feature);
            while (edge.HasBoundary) edge.Release(EdgeConstraintKind.Boundary);
        }
    }

    readonly record struct ConstrainedSpoke(
        Node Other,
        Edge[] HalfEdges);

    readonly record struct ConstraintCounts(int Features, int Boundaries);

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
        if (Traversal.Root.Dead)
        {
            Face replacement = affectedFaces.FirstOrDefault(face => !face.Dead)
                ?? throw new InvalidOperationException(
                    "A topology change retired the root without a replacement face.");
            _mesh.SetRoot(replacement);
            Traversal.SetRoot(replacement);
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

        new FaceClassifier(_mesh, Traversal, _superStructure).Classify();
    }

    void SynchronizeTopology()
    {
        if (Traversal.Root.Dead)
        {
            Traversal.SetRoot(_mesh.Root);
        }
        _locator.Reset();
    }
}
