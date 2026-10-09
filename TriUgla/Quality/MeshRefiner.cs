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
    readonly Dictionary<SegmentKey, Edge> _segments = new(SegmentKeyComparer.Instance);
    readonly Queue<Edge> _edges = new();
    readonly Queue<Face> _faces = new();
    readonly Dictionary<FaceFailureKey, int> _unchangedFailures = new(FaceFailureKeyComparer.Instance);
    FaceFailureReason? _lastFailureReason;
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
            int consecutiveOutsideDeferrals = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (settings.UseSteinerBudget && inserted >= settings.MaxSteiners &&
                    (_edges.Count > 0 || _faces.Count > 0))
                    return Result(RefineStatus.SteinerBudgetReached, inserted, ranker, settings,
                        "The configured Steiner-node budget was reached before refinement completed.");

                if (TryDequeueEdge(out Edge edge))
                {
                    if (ProcessEncroachedSegment(edge, ranker, settings))
                    {
                        inserted++;
                        consecutiveOutsideDeferrals = 0;
                    }
                    continue;
                }

                if (TryDequeueFace(out Face face))
                {
                    FaceProcessResult processed = TryProcessBadFace(face, ranker, settings);
                    if (processed != FaceProcessResult.OutsideDeferred)
                        consecutiveOutsideDeferrals = 0;
                    if (processed == FaceProcessResult.Inserted)
                    {
                        inserted++;
                    }
                    else if (processed == FaceProcessResult.Failed) EnqueueFace(face);
                    else if (processed == FaceProcessResult.OutsideDeferred)
                    {
                        EnqueueFace(face);
                        consecutiveOutsideDeferrals++;
                        if (_edges.Count == 0 && consecutiveOutsideDeferrals >= _faces.Count)
                            return Result(RefineStatus.Completed, inserted, ranker, settings, null);
                    }
                    else if (processed == FaceProcessResult.Stagnated)
                        return Result(RefineStatus.NumericalStagnation, inserted, ranker, settings,
                            $"A bad face repeatedly failed without a topology change " +
                            $"({_lastFailureReason}).");
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
        if (LandsOutsideDomain(hit))
            return FaceProcessResult.OutsideDeferred;
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
        _segments.Remove(originalSegment);
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
        => _segments.TryAdd(new SegmentKey(edge.NodeStart, edge.NodeEnd), edge);

    int EnqueueEncroached(Vec2 point)
    {
        int count = 0;
        foreach (Edge edge in _segments.Values)
        {
            if (Encroached(edge, point) && VisibleFromInterior(edge, point))
            {
                EnqueueEdge(edge);
                count++;
            }
        }
        return count;
    }

    bool VisibleFromInterior(Edge edge, Vec2 point)
    {
        Vec2 midpoint = Candidate(edge);
        foreach (Edge other in _segments.Values)
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
        _lastFailureReason = null;
        var illegalEdges = new Queue<Edge>(change.EdgesToLegalize.Where(edge => !edge.Dead));
        EdgeLegalizationResult legalization = legalizer.Legalize(illegalEdges);

        var affected = new HashSet<Face>(ReferenceEqualityComparer.Instance);
        affected.UnionWith(change.AffectedFaces);
        affected.UnionWith(legalization.AffectedFaces);

        foreach (Face face in affected)
        {
            if (!Processable(face, settings)) continue;
            double badness = ranker.Rank(face);
            if (IsBad(badness)) EnqueueFace(face);
        }

    }

    static bool LandsOutsideDomain(LocateResult hit)
    {
        if (hit.IsEmpty || hit.Face?.Kind == FaceKind.Outside) return true;
        if (hit.Edge is Edge edge)
        {
            if (edge.Face.Kind != FaceKind.Outside) return false;
            return edge.Twin is null || edge.Twin.Face.Kind == FaceKind.Outside;
        }
        if (hit.Node is not Node node) return false;
        foreach (Edge incident in IncidentEdges(node))
        {
            if (incident.Face.Kind != FaceKind.Outside ||
                incident.Twin is Edge twin && twin.Face.Kind != FaceKind.Outside) return false;
        }
        return true;
    }

    void EnqueueEdge(Edge edge)
        => _edges.Enqueue(edge);

    void EnqueueFace(Face face)
        => _faces.Enqueue(face);

    bool TryDequeueEdge(out Edge edge)
    {
        if (_edges.TryDequeue(out Edge? queued))
        {
            edge = queued;
            return true;
        }

        edge = null!;
        return false;
    }

    bool TryDequeueFace(out Face face)
    {
        if (_faces.TryDequeue(out Face? queued))
        {
            face = queued;
            return true;
        }

        face = null!;
        return false;
    }

    void Clear()
    {
        _segments.Clear();
        _edges.Clear();
        _faces.Clear();
        _unchangedFailures.Clear();
        _lastFailureReason = null;
    }

    int Reconcile(
        FaceRanker ranker,
        in RefineSettings settings,
        CancellationToken cancellationToken)
    {
        int discovered = 0;
        foreach (Edge edge in _segments.Values.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (edge.Dead || !edge.OrTwinConstrained) continue;
            if (!EncroachedInvariant(edge)) continue;
            EnqueueEdge(edge);
            discovered++;
        }

        return discovered;
    }

    FaceProcessResult RecordFailure(
        Face face,
        FaceFailureReason reason,
        in RefineSettings settings)
    {
        var key = FaceFailureKey.From(face, reason);
        _lastFailureReason = reason;
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
        int badFaces = _faces.Count(face =>
            Processable(face, criteria) && IsBad(ranker.Rank(face)));
        if (status == RefineStatus.NumericalStagnation && badFaces == 0) badFaces = 1;
        int encroached = _segments.Values.Count(edge =>
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

    enum FaceProcessResult { Ignored, Deferred, OutsideDeferred, Failed, Inserted, Stagnated }
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
