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
    IMeshLocator locator,
    IEdgeLegalizer legalizer,
    ISplitter splitter,
    INodeInserter nodeInserter,
    MeshTraversal traversal)
{
    readonly HashSet<Edge> _segments = new(ReferenceEqualityComparer.Instance);
    readonly Queue<Edge> _edgeQueue = new();
    readonly HashSet<Edge> _queuedEdges = new(ReferenceEqualityComparer.Instance);
    readonly Queue<Face> _faceQueue = new();
    readonly HashSet<Face> _queuedFaces = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<Face, FaceProgress> _faceProgress = new(ReferenceEqualityComparer.Instance);

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
        Validate(settings);
        Clear();
        FillQueues(faces, ranker, settings);

        int inserted = 0;
        while ((_edgeQueue.Count > 0 || _faceQueue.Count > 0) &&
               (!settings.UseSteinerBudget || inserted < settings.MaxSteiners))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryDequeueEdge(out Edge edge))
            {
                if (ProcessEncroachedSegment(edge, ranker, settings)) inserted++;
                continue;
            }

            if (TryDequeueFace(out Face face) && ProcessBadFace(face, ranker, settings))
            {
                inserted++;
            }
        }
        return inserted;
    }

    public async ValueTask<int> RefineAsync(
        IEnumerable<Face> faces,
        FaceRanker ranker,
        RefineSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(faces);
        ArgumentNullException.ThrowIfNull(ranker);
        Validate(settings);
        Clear();
        FillQueues(faces, ranker, settings);

        int inserted = 0;
        int operations = 0;
        while ((_edgeQueue.Count > 0 || _faceQueue.Count > 0) &&
               (!settings.UseSteinerBudget || inserted < settings.MaxSteiners))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((++operations & 31) == 0)
            {
                // Blazor WebAssembly normally shares the browser UI thread. Yielding
                // periodically lets click events (especially Stop) run during refinement.
                await Task.Yield();
            }

            if (TryDequeueEdge(out Edge edge))
            {
                if (ProcessEncroachedSegment(edge, ranker, settings)) inserted++;
                continue;
            }

            if (TryDequeueFace(out Face face) && ProcessBadFace(face, ranker, settings))
            {
                inserted++;
            }
        }
        return inserted;
    }

    public bool ProcessBadFace(Face face, FaceRanker ranker, in RefineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(ranker);
        if (!Processable(face)) return false;

        double badness = ranker.Rank(face);
        if (badness <= 0d) return false;
        if (!ShouldProcessFace(face, badness, settings)) return false;
        if (!TryCircumcenter(face, out Vec2 candidate)) return false;

        LocateResult hit = locator.Locate(candidate, face);
        // A numerically degenerate face can yield a finite circumcenter just
        // outside the represented topology. It is not a fatal mesh error: leave
        // the face unchanged and let progress policy decide future attempts.
        if (hit.IsEmpty) return false;

        if (EnqueueEncroached(candidate) > 0)
        {
            EnqueueFace(face);
            return false;
        }

        InsertNodeResult insertion = nodeInserter.Insert(candidate, face);
        TopologyChange? change = insertion.FaceSplit?.Change ?? insertion.EdgeSplit?.Change;
        if (change is null) return false;

        DrainAffected(change.Value, ranker, settings);
        return true;
    }

    public bool ProcessEncroachedSegment(
        Edge edge,
        FaceRanker ranker,
        in RefineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (edge.Dead || !edge.OrTwinConstrained) return false;

        var node = new Node { Position = Candidate(edge) };
        node.Data = Barycentric.FromSegment(
            node.Position,
            edge.NodeStart.Position,
            edge.NodeEnd.Position).Interpolate(
                edge.NodeStart.Data,
                edge.NodeEnd.Data,
                default);

        EdgeSplitResult split = splitter.Split(edge, node);
        _segments.Remove(edge);
        if (edge.Twin is not null) _segments.Remove(edge.Twin);
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
        foreach (Node node in traversal.Nodes())
        {
            if (!edge.Contains(node) &&
                Encroached(edge, node.Position) &&
                VisibleFromInterior(edge, node.Position)) return true;
        }
        return false;
    }

    public bool Encroached(Edge edge, Vec2 point)
        => geometry.InDiameterCircle(edge.NodeStart, edge.NodeEnd, point);

    void FillQueues(IEnumerable<Face> faces, FaceRanker ranker, in RefineSettings settings)
    {
        foreach (Face face in faces)
        {
            if (Processable(face))
            {
                double badness = ranker.Rank(face);
                if (badness > 0d) EnqueueFace(face);
            }

        }

        // Segment protection is global even when callers refine only a subset of
        // faces. A rejected circumcenter may encroach any visible PSLG subsegment,
        // including one whose incident faces were not selected for quality work.
        foreach (Edge edge in traversal.Edges())
        {
            if (!edge.OrTwinConstrained || !AddSegment(edge)) continue;
            if (EncroachedInvariant(edge)) EnqueueEdge(edge);
        }
    }

    bool AddSegment(Edge edge)
    {
        foreach (Edge existing in _segments)
        {
            if (existing.Contains(edge.NodeStart) && existing.Contains(edge.NodeEnd)) return false;
        }
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
            if (Intersection.Intersect(
                    other.NodeStart.Position,
                    other.NodeEnd.Position,
                    midpoint,
                    point,
                    out _)) return false;
        }
        return true;
    }

    void DrainAffected(TopologyChange change, FaceRanker ranker, in RefineSettings settings)
    {
        var illegalEdges = new Queue<Edge>(change.EdgesToLegalize.Where(edge => !edge.Dead));
        EdgeLegalizationResult legalization = legalizer.Legalize(illegalEdges);

        foreach (Face face in change.AffectedFaces.Concat(legalization.AffectedFaces))
        {
            if (!Processable(face)) continue;
            double badness = ranker.Rank(face);
            if (badness > 0d) EnqueueFace(face);
        }
    }

    bool ShouldProcessFace(Face face, double badness, in RefineSettings settings)
    {
        // Record progress only when a face is actually dequeued. Queue discovery
        // must not consume its first attempt. Face objects are intentionally reused
        // by topology operations, so an improvement resets their stagnation count.
        if (!_faceProgress.TryGetValue(face, out FaceProgress progress))
        {
            _faceProgress.Add(face, new FaceProgress(badness, 0));
            return true;
        }

        if (badness + settings.ImproveEps < progress.BestBadness)
        {
            _faceProgress[face] = new FaceProgress(badness, 0);
            return true;
        }

        progress = progress with { Stagnation = progress.Stagnation + 1 };
        _faceProgress[face] = progress;
        return settings.ContinueOnFaceStagnation ||
               progress.Stagnation <= settings.FaceStagnationBudget;
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
        _edgeQueue.Clear();
        _queuedEdges.Clear();
        _faceQueue.Clear();
        _queuedFaces.Clear();
        _faceProgress.Clear();
    }

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

    static bool Processable(Face face)
        => !face.Dead && face.Kind is FaceKind.Undefined or FaceKind.Island;

    static void Validate(in RefineSettings settings)
    {
        if (settings.MaxSteiners < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        if (settings.FaceStagnationBudget < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        if (!double.IsFinite(settings.ImproveEps) || settings.ImproveEps < 0d)
            throw new ArgumentOutOfRangeException(nameof(settings));
    }

    readonly record struct FaceProgress(double BestBadness, int Stagnation);
}
