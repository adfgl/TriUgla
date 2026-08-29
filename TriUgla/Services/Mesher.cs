namespace TriUgla;

public sealed class Mesher
{
    readonly List<Constraint> _constraints = [];
    readonly List<Loop> _loops = [];
    readonly Mesh _mesh;
    readonly MeshLocator _locator;
    readonly INodeInserter _nodeInserter;
    readonly INodeRemover _nodeRemover;
    readonly IEdgeInserter _edgeInserter;
    readonly IEdgeLegalizer _edgeLegalizer;
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
    public IReadOnlyList<Constraint> Constraints => _constraints;
    public IReadOnlyList<Loop> Loops => _loops;

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
        if (change is not null) TopologyChanged(change.Value.AffectedFaces);
        return result;
    }

    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!Traversal.Nodes().Any(candidate => ReferenceEquals(candidate, node)))
        {
            return RemoveNodeResult.Failed(node);
        }

        RemoveNodeResult result = _nodeRemover.Remove(node);
        if (result.Removed) TopologyChanged(result.Change.AffectedFaces);
        return result;
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

    public bool TryInsertConstraint(Constraint constraint, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        if (!ValidateConstraint(constraint, out reason)) return false;

        foreach (ConstraintSpan span in constraint.Spans)
        {
            InsertEdge(span.From, span.To, EdgeConstraintKind.Feature);
        }

        foreach (ConstraintPoint point in constraint.Points)
        {
            point.Node.Constrain();
        }

        _constraints.Add(constraint);
        reason = null;
        return true;
    }

    public bool TryRemoveConstraint(Constraint constraint, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        int index = _constraints.IndexOf(constraint);
        if (index < 0) return Fail(out reason, $"{ConstraintContext(constraint)} not found in mesh.");

        var paths = new List<List<Edge>>(constraint.Spans.Count);
        for (int i = 0; i < constraint.Spans.Count; i++)
        {
            if (!TryResolvePath(constraint.Spans[i], out List<Edge> path, out string? pathReason))
                return Fail(out reason, $"{ConstraintContext(constraint)} span[{i}]: {pathReason}");
            if (path.Count == 0)
                return Fail(out reason, $"{ConstraintContext(constraint)} span[{i}]: produced no edges.");
            if (path.Any(edge => !edge.HasFeature))
                return Fail(out reason, $"{ConstraintContext(constraint)} span[{i}]: edge in path has no Feature constraint.");
            paths.Add(path);
        }

        for (int i = 0; i < constraint.Points.Count; i++)
        {
            Node node = constraint.Points[i].Node;
            if (!node.Constrained)
                return Fail(out reason, $"{ConstraintContext(constraint)} point[{i}]: node is not constrained.");
        }

        ReleasePaths(paths, EdgeConstraintKind.Feature);
        foreach (ConstraintPoint point in constraint.Points) point.Node.Relax();
        _constraints.RemoveAt(index);
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

        for (int i = 0; i < loop.Nodes.Count - 1; i++)
            InsertEdge(loop.Nodes[i], loop.Nodes[i + 1], EdgeConstraintKind.Boundary);

        _loops.Add(loop);
        reason = null;
        return true;
    }

    public bool TryRemoveLoop(Loop loop, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(loop);
        int index = _loops.IndexOf(loop);
        if (index < 0) return Fail(out reason, $"{LoopContext(loop)} not found in mesh.");

        loop.Close();
        var paths = new List<List<Edge>>(loop.Nodes.Count - 1);
        for (int i = 0; i < loop.Nodes.Count - 1; i++)
        {
            var span = new ConstraintSpan(loop.Nodes[i], loop.Nodes[i + 1]);
            if (!TryResolvePath(span, out List<Edge> path, out string? pathReason))
                return Fail(out reason, $"{LoopContext(loop)} edge[{i}]: {pathReason}");
            if (path.Count == 0 || path.Any(edge => !edge.HasBoundary))
                return Fail(out reason, $"{LoopContext(loop)} edge[{i}] has no Boundary constraint.");
            paths.Add(path);
        }

        ReleasePaths(paths, EdgeConstraintKind.Boundary);
        _loops.RemoveAt(index);
        reason = null;
        return true;
    }

    void InsertEdge(Node start, Node end, EdgeConstraintKind kind)
    {
        EdgeInsertResult result = _edgeInserter.Insert(start, end, kind);
        TopologyChanged(result.Change.AffectedFaces);
        Legalize(result.Change.EdgesToLegalize);
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

    void Legalize(IEnumerable<Edge> candidates)
    {
        var queue = candidates as Queue<Edge> ?? new Queue<Edge>(candidates);
        if (queue.Count == 0) return;
        EdgeLegalizationResult result = _edgeLegalizer.Legalize(queue);
        TopologyChanged(result.AffectedFaces);
    }

    bool ValidateConstraint(Constraint constraint, out string? reason)
    {
        for (int i = 0; i < constraint.Spans.Count; i++)
        {
            ConstraintSpan span = constraint.Spans[i];
            if (!ValidateNode(span.From, out string fromWhy))
                return Fail(out reason, $"{ConstraintContext(constraint)} span[{i}] From: {fromWhy}");
            if (!ValidateNode(span.To, out string toWhy))
                return Fail(out reason, $"{ConstraintContext(constraint)} span[{i}] To: {toWhy}");
            if (ReferenceEquals(span.From, span.To) || span.From.Position == span.To.Position)
                return Fail(out reason, $"{ConstraintContext(constraint)} span[{i}]: endpoints must be distinct.");
        }
        for (int i = 0; i < constraint.Points.Count; i++)
        {
            if (!ValidateNode(constraint.Points[i].Node, out string why))
                return Fail(out reason, $"{ConstraintContext(constraint)} point[{i}]: {why}");
        }
        reason = null;
        return true;
    }

    bool ValidateNode(Node node, out string why)
    {
        if (node.Dead) { why = "node is invalid."; return false; }
        if (_superStructure?.SuperNode(node) == true)
        { why = "node is part of the super structure."; return false; }
        if (!Traversal.Nodes().Any(candidate => ReferenceEquals(candidate, node)))
        { why = "node does not belong to this mesh."; return false; }
        why = string.Empty;
        return true;
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

    static bool TryResolvePath(ConstraintSpan span, out List<Edge> path, out string? reason)
    {
        path = [];
        try { span.Edges(path); reason = null; return true; }
        catch (InvalidOperationException exception) { reason = exception.Message; return false; }
    }

    static bool Fail(out string? reason, string message) { reason = message; return false; }
    static string ConstraintContext(Constraint constraint) => $"Constraint '{constraint.Name}'";
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
