namespace TriUgla;

/// <summary>
/// Ruppert-style constrained Delaunay refinement: encroached subsegments are
/// bisected before bad triangles, and a bad triangle's circumcenter is inserted
/// only when it does not encroach upon a visible constrained subsegment.
/// </summary>
/// <remarks>
/// Dwyer's divide-and-conquer work concerns construction of the initial Delaunay
/// triangulation; the refinement ordering implemented here is Ruppert's segment-
/// before-triangle priority. Robust signs are delegated to <see cref="IGeometry"/>.
/// </remarks>
public sealed class MeshRefiner(
    IGeometry geometry,
    MeshLocator locator,
    EdgeLegalizer legalizer,
    Splitter splitter,
    NodeInserter nodeInserter)
{
    readonly HashSet<Edge> _segments = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<SegmentKey> _segmentKeys = new(SegmentKeyComparer.Instance);
    readonly Queue<Edge> _edgeQueue = new();
    readonly HashSet<Edge> _queuedEdges = new(ReferenceEqualityComparer.Instance);
    readonly Queue<Face> _faceQueue = new();
    readonly HashSet<Face> _queuedFaces = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Face> _domainFaces = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<FaceFailureKey, int> _unchangedFailures = new(FaceFailureKeyComparer.Instance);
    int _refining;

    public int Refine(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings)
        => RefineDetailed(faces, ranker, in settings, CancellationToken.None).InsertedNodes;

    public int Refine(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
        => RefineDetailed(faces, ranker, in settings, cancellationToken).InsertedNodes;

    public RefineResult RefineDetailed(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(faces);
        ArgumentNullException.ThrowIfNull(ranker);
        Validate(settings);
        EnterRefinement();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Clear();
            FillQueues(faces, ranker, settings, cancellationToken);

            int inserted = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (settings.UseSteinerBudget && inserted >= settings.MaxSteiners &&
                    (_edgeQueue.Count > 0 || _faceQueue.Count > 0))
                    return Result(RefineStatus.SteinerBudgetReached, inserted, ranker, settings,
                        "The configured Steiner-node budget was reached before refinement completed.");

                if (TryDequeueEdge(out Edge edge))
                {
                    if (ProcessEncroachedSegment(edge, ranker, settings)) inserted++;
                    continue;
                }

                if (TryDequeueFace(out Face face))
                {
                    FaceProcessResult processed = TryProcessBadFace(face, ranker, settings);
                    if (processed == FaceProcessResult.Inserted) inserted++;
                    else if (processed == FaceProcessResult.Stagnated)
                        return Result(RefineStatus.NumericalStagnation, inserted, ranker, settings,
                            "A bad face repeatedly failed without a topology change.");
                    continue;
                }

                if (Reconcile(ranker, settings, cancellationToken) == 0)
                    return Result(RefineStatus.Completed, inserted, ranker, settings, null);
            }
        }
        finally
        {
            ExitRefinement();
        }
    }

    public bool ProcessBadFace(Face face, FaceRanker ranker, in RefineSettings settings)
        => TryProcessBadFace(face, ranker, settings) == FaceProcessResult.Inserted;

    FaceProcessResult TryProcessBadFace(Face face, FaceRanker ranker, in RefineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(ranker);
        if (!Processable(face, settings)) return FaceProcessResult.Ignored;

        double badness = ranker.Rank(face);
        if (!IsBad(badness)) return FaceProcessResult.Ignored;
        if (!TryCircumcenter(face, out Vec2 candidate))
            return RecordFailure(face, FaceFailureReason.NonFiniteCandidate, settings);

        LocateResult hit = locator.Locate(candidate, face);
        // A numerically degenerate face can yield a finite circumcenter just
        // outside the represented topology. It is not a fatal mesh error: leave
        // the face unchanged and let progress policy decide future attempts.
        if (hit.IsEmpty) return RecordFailure(face, FaceFailureReason.OutsideTopology, settings);
        if (hit.IsNode) return RecordFailure(face, FaceFailureReason.ExistingNode, settings);

        if (EnqueueEncroached(candidate) > 0)
        {
            EnqueueFace(face);
            return FaceProcessResult.Deferred;
        }

        InsertNodeResult insertion = nodeInserter.Insert(candidate, face);
        TopologyChange? change = insertion.FaceSplit?.Change ?? insertion.EdgeSplit?.Change;
        if (change is null) return RecordFailure(face, FaceFailureReason.NoTopologyChange, settings);
        insertion.Node!.Kind = NodeKind.SteinerRefinement;

        DrainAffected(change.Value, ranker, settings);
        return FaceProcessResult.Inserted;
    }

    public bool ProcessEncroachedSegment(
        Edge edge,
        FaceRanker ranker,
        in RefineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (edge.Dead || !edge.OrTwinConstrained) return false;

        var node = new Node
        {
            Position = Candidate(edge),
            Kind = NodeKind.SteinerRefinement
        };
        node.Data = Barycentric.FromSegment(
            node.Position,
            edge.NodeStart.Position,
            edge.NodeEnd.Position).Interpolate(
                edge.NodeStart.Data,
                edge.NodeEnd.Data,
                default);

        var originalSegment = new SegmentKey(edge.NodeStart, edge.NodeEnd);
        EdgeSplitResult split = splitter.Split(edge, node);
        _segments.Remove(edge);
        if (edge.Twin is not null) _segments.Remove(edge.Twin);
        _segmentKeys.Remove(originalSegment);
        AddSegment(split.FirstHalf);
        AddSegment(split.SecondHalf);

        if (EncroachedInvariant(split.FirstHalf)) EnqueueEdge(split.FirstHalf);
        if (EncroachedInvariant(split.SecondHalf)) EnqueueEdge(split.SecondHalf);
        DrainAffected(split.Change, ranker, settings);
        return true;
    }

    public bool EncroachedInvariant(Edge edge)
        => Encroached(edge) || edge.Twin is not null && Encroached(edge.Twin);

    public bool Encroached(Edge edge)
    {
        foreach (Edge incident in IncidentEdges(edge.NodeStart))
        {
            if (EncroachedBy(edge, incident.NodeEnd)) return true;
        }
        foreach (Edge incident in IncidentEdges(edge.NodeEnd))
        {
            if (EncroachedBy(edge, incident.NodeEnd)) return true;
        }
        return false;
    }

    public bool Encroached(Edge edge, Vec2 point)
        => geometry.InDiameterCircle(edge.NodeStart, edge.NodeEnd, point);

    void FillQueues(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
    {
        foreach (Face face in faces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _domainFaces.Add(face);
            if (Processable(face, settings))
            {
                double badness = ranker.Rank(face);
                if (IsBad(badness)) EnqueueFace(face);
            }

            foreach (Edge edge in face.Edges)
            {
                if (!edge.OrTwinConstrained || !AddSegment(edge)) continue;
                if (EncroachedInvariant(edge)) EnqueueEdge(edge);
            }
        }
    }

    bool AddSegment(Edge edge)
    {
        if (!_segmentKeys.Add(new SegmentKey(edge.NodeStart, edge.NodeEnd))) return false;
        return _segments.Add(edge);
    }

    int EnqueueEncroached(Vec2 point)
    {
        int count = 0;
        foreach (Edge edge in _segments)
        {
            if (Encroached(edge, point) && VisibleFromInterior(edge, point))
            {
                if (EnqueueEdge(edge)) count++;
            }
        }
        return count;
    }

    bool VisibleFromInterior(Edge edge, Vec2 point)
    {
        Vec2 midpoint = Candidate(edge);
        foreach (Edge other in _segments)
        {
            if (SameOrAdjacent(edge, other)) continue;
            if (geometry.Intersects(
                    other.NodeStart.Position,
                    other.NodeEnd.Position,
                    midpoint,
                    point) >= 0) return false;
        }
        return true;
    }

    void DrainAffected(TopologyChange change, FaceRanker ranker, in RefineSettings settings)
    {
        // Any successful topology mutation is genuine progress. Previously
        // recorded failure signatures no longer describe the current mesh.
        _unchangedFailures.Clear();
        var illegalEdges = new Queue<Edge>(change.EdgesToLegalize.Where(edge => !edge.Dead));
        EdgeLegalizationResult legalization = legalizer.Legalize(illegalEdges);

        var affected = new HashSet<Face>(ReferenceEqualityComparer.Instance);
        affected.UnionWith(change.AffectedFaces);
        affected.UnionWith(legalization.AffectedFaces);

        foreach (Face face in affected)
        {
            _domainFaces.Add(face);
            if (!Processable(face, settings)) continue;
            double badness = ranker.Rank(face);
            if (IsBad(badness)) EnqueueFace(face);
        }
    }

    bool EnqueueEdge(Edge edge)
    {
        if (!_queuedEdges.Add(edge)) return false;
        _edgeQueue.Enqueue(edge);
        return true;
    }

    void EnqueueFace(Face face)
    {
        if (_queuedFaces.Add(face)) _faceQueue.Enqueue(face);
    }

    bool TryDequeueEdge(out Edge edge)
    {
        if (!_edgeQueue.TryDequeue(out Edge? found))
        {
            edge = null!;
            return false;
        }
        edge = found;
        _queuedEdges.Remove(edge);
        return true;
    }

    bool TryDequeueFace(out Face face)
    {
        if (!_faceQueue.TryDequeue(out Face? found))
        {
            face = null!;
            return false;
        }
        face = found;
        _queuedFaces.Remove(face);
        return true;
    }

    void Clear()
    {
        _segments.Clear();
        _segmentKeys.Clear();
        _edgeQueue.Clear();
        _queuedEdges.Clear();
        _faceQueue.Clear();
        _queuedFaces.Clear();
        _domainFaces.Clear();
        _unchangedFailures.Clear();
    }

    int Reconcile(
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
    {
        int discovered = 0;
        foreach (Edge edge in _segments.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (edge.Dead || !edge.OrTwinConstrained) continue;
            if (EncroachedInvariant(edge) && EnqueueEdge(edge)) discovered++;
        }

        foreach (Face face in _domainFaces.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Processable(face, settings) || !IsBad(ranker.Rank(face))) continue;
            if (_queuedFaces.Add(face))
            {
                _faceQueue.Enqueue(face);
                discovered++;
            }
        }
        return discovered;
    }

    FaceProcessResult RecordFailure(
        Face face,
        FaceFailureReason reason,
        in RefineSettings settings)
    {
        var key = FaceFailureKey.From(face, reason);
        int failures = _unchangedFailures.TryGetValue(key, out int count) ? count + 1 : 1;
        _unchangedFailures[key] = failures;
        return failures > settings.UnchangedFailureBudget
            ? FaceProcessResult.Stagnated
            : FaceProcessResult.Failed;
    }

    RefineResult Result(
        RefineStatus status,
        int inserted,
        FaceRanker ranker,
        in RefineSettings settings,
        string? reason)
    {
        RefineSettings criteria = settings;
        int badFaces = _domainFaces.Count(face =>
            Processable(face, criteria) && IsBad(ranker.Rank(face)));
        int encroached = _segments.Count(edge =>
            !edge.Dead && edge.OrTwinConstrained && EncroachedInvariant(edge));
        return new RefineResult(status, inserted, badFaces, encroached, reason);
    }

    bool EncroachedBy(Edge edge, Node node)
        => !edge.Contains(node) &&
           Encroached(edge, node.Position) &&
           VisibleFromInterior(edge, node.Position);

    static IEnumerable<Edge> IncidentEdges(Node node)
    {
        Edge first = node.Edge;
        if (first is null) yield break;

        Edge current = first;
        bool closed = false;
        do
        {
            yield return current;
            Edge? next = current.Prev.Twin;
            if (next is null) break;
            current = next;
            closed = ReferenceEquals(current, first);
        }
        while (!closed);

        if (closed) yield break;

        current = first;
        while (current.Twin is not null)
        {
            current = current.Twin.Next;
            if (ReferenceEquals(current, first)) yield break;
            yield return current;
        }
    }

    void EnterRefinement()
    {
        if (Interlocked.CompareExchange(ref _refining, 1, 0) != 0)
            throw new InvalidOperationException(
                "This mesh refiner is already running a refinement operation.");
    }

    void ExitRefinement() => Volatile.Write(ref _refining, 0);

    static Vec2 Candidate(Edge edge)
        => Vec2.Lerp(edge.NodeStart.Position, edge.NodeEnd.Position, 0.5d);

    static bool TryCircumcenter(Face face, out Vec2 center)
    {
        Edge edge = face.Edge;
        Circle circle = Circle.From3(
            edge.NodeStart.Position,
            edge.NodeEnd.Position,
            edge.Next.NodeEnd.Position);
        center = circle.Center;
        return double.IsFinite(center.X) && double.IsFinite(center.Y);
    }

    static bool SameOrAdjacent(Edge first, Edge second)
        => ReferenceEquals(first, second) ||
           first.Contains(second.NodeStart) ||
           first.Contains(second.NodeEnd);

    static bool Processable(Face face, in RefineSettings settings)
        => !face.Dead && face.Kind switch
        {
            FaceKind.Undefined => true,
            FaceKind.Island => settings.RefineLand,
            FaceKind.Lake => settings.RefineLakes,
            _ => false
        };

    static bool IsBad(double badness)
        => double.IsFinite(badness) && badness > 0d;

    static void Validate(in RefineSettings settings)
    {
        if (settings.MaxSteiners < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        if (settings.FaceStagnationBudget < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        if (settings.UnchangedFailureBudget < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        if (!double.IsFinite(settings.ImproveEps) || settings.ImproveEps < 0d)
            throw new ArgumentOutOfRangeException(nameof(settings));
    }

    enum FaceProcessResult { Ignored, Deferred, Failed, Inserted, Stagnated }
    enum FaceFailureReason { NonFiniteCandidate, OutsideTopology, ExistingNode, NoTopologyChange }

    readonly record struct FaceFailureKey(Node A, Node B, Node C, FaceFailureReason Reason)
    {
        public static FaceFailureKey From(Face face, FaceFailureReason reason)
        {
            Edge edge = face.Edge;
            return new FaceFailureKey(edge.NodeStart, edge.NodeEnd, edge.Next.NodeEnd, reason);
        }
    }

    sealed class FaceFailureKeyComparer : IEqualityComparer<FaceFailureKey>
    {
        public static readonly FaceFailureKeyComparer Instance = new();

        public bool Equals(FaceFailureKey x, FaceFailureKey y)
            => x.Reason == y.Reason && SameTriangle(x, y);

        static bool SameTriangle(FaceFailureKey x, FaceFailureKey y)
            => Contains(y, x.A) && Contains(y, x.B) && Contains(y, x.C);

        static bool Contains(FaceFailureKey key, Node node)
            => ReferenceEquals(key.A, node) || ReferenceEquals(key.B, node) ||
               ReferenceEquals(key.C, node);

        public int GetHashCode(FaceFailureKey key)
            => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key.A) ^
               System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key.B) ^
               System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key.C) ^
               (int)key.Reason;
    }

    readonly record struct SegmentKey(Node First, Node Second);

    sealed class SegmentKeyComparer : IEqualityComparer<SegmentKey>
    {
        public static readonly SegmentKeyComparer Instance = new();

        public bool Equals(SegmentKey x, SegmentKey y)
            => ReferenceEquals(x.First, y.First) && ReferenceEquals(x.Second, y.Second) ||
               ReferenceEquals(x.First, y.Second) && ReferenceEquals(x.Second, y.First);

        public int GetHashCode(SegmentKey key)
            => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key.First) ^
               System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key.Second);
    }
}
