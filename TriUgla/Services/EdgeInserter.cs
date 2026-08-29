namespace TriUgla;

public sealed class EdgeInserter(
    IGeometry geometry,
    IEdgeFlipper flipper,
    ISplitter splitter,
    INodeFactory nodes) : IEdgeInserter
{
    const int MaximumOperations = 100_000;

    public bool SplitCrossedEdges { get; set; }

    public EdgeInsertResult Insert(
        Node start,
        Node end,
        EdgeConstraintKind kind = EdgeConstraintKind.Feature)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);

        var constrained = new List<Edge>(8);
        var insertedNodes = new List<Node>();
        var affectedFaces = new HashSet<Face>();
        var edgesToLegalize = new List<Edge>();
        var segments = new SegmentQueue();
        segments.Enqueue(start, end);

        int operations = 0;
        while (segments.TryDequeue(out Node segmentStart, out Node segmentEnd))
        {
            if (++operations > MaximumOperations)
            {
                throw new InvalidOperationException(
                    "Edge insertion did not converge. The mesh may contain invalid topology.");
            }

            Edge entrance = FindEntrance(segmentStart, segmentEnd)
                ?? throw new InvalidOperationException(
                    $"Cannot find a face leaving node at {segmentStart.Position} " +
                    $"toward {segmentEnd.Position}.");

            if (ReferenceEquals(entrance.NodeEnd, segmentEnd))
            {
                Constrain(constrained, entrance, kind);
                continue;
            }

            if (ContinuesAlongSegment(entrance, segmentEnd))
            {
                Constrain(constrained, entrance, kind);
                segments.Enqueue(entrance.NodeEnd, segmentEnd);
                continue;
            }

            Edge crossed = entrance.Next;
            if (CanRemoveByFlipping(crossed))
            {
                EdgeFlipResult flip = flipper.Flip(crossed);
                AddResult(flip.Change, affectedFaces, edgesToLegalize);
                segments.Enqueue(segmentStart, segmentEnd);
                continue;
            }

            Vec2 intersection = FindIntersection(segmentStart, segmentEnd, crossed);
            Node inserted = nodes.Create(
                intersection,
                LocateResult.From(crossed));
            EdgeSplitResult split = splitter.Split(crossed, inserted);

            insertedNodes.Add(inserted);
            AddResult(split.Change, affectedFaces, edgesToLegalize);
            segments.Enqueue(segmentStart, inserted);
            segments.Enqueue(inserted, segmentEnd);
        }

        return new EdgeInsertResult(
            constrained,
            insertedNodes,
            new TopologyChange(
                affectedFaces.ToArray(),
                edgesToLegalize));
    }

    bool CanRemoveByFlipping(Edge edge)
        => !SplitCrossedEdges && flipper.CanFlip(edge, out _);

    bool ContinuesAlongSegment(Edge edge, Node end)
    {
        if (geometry.Orient(edge, end.Position) != EOrientaiton.Collinear)
        {
            return false;
        }

        Vec2 segment = end.Position - edge.NodeStart.Position;
        Vec2 candidate = edge.NodeEnd.Position - edge.NodeStart.Position;
        return segment.Dot(candidate) > 0 &&
               candidate.LengthSquared <= segment.LengthSquared;
    }

    Edge? FindEntrance(Node start, Node end)
    {
        if (start.Edge is null)
        {
            return null;
        }

        Edge first = start.Edge;
        Edge current = first;

        do
        {
            Edge previous = current.Prev;
            EOrientaiton currentSide = geometry.Orient(current, end.Position);
            EOrientaiton previousSide = geometry.Orient(previous, end.Position);

            if (currentSide == EOrientaiton.Collinear &&
                previousSide == EOrientaiton.Counterclockwise)
            {
                return current;
            }

            if (currentSide == EOrientaiton.Counterclockwise &&
                previousSide == EOrientaiton.Counterclockwise)
            {
                return current;
            }

            Edge? next = previous.Twin;
            if (currentSide == EOrientaiton.Counterclockwise &&
                previousSide == EOrientaiton.Collinear)
            {
                return next;
            }

            if (next is null)
            {
                return null;
            }

            current = next;
        }
        while (!ReferenceEquals(first, current));

        return null;
    }

    static Vec2 FindIntersection(Node start, Node end, Edge crossed)
    {
        if (!Intersection.Intersect(
                start.Position,
                end.Position,
                crossed.NodeStart.Position,
                crossed.NodeEnd.Position,
                out Vec2 intersection))
        {
            throw new InvalidOperationException(
                "The selected crossed edge does not intersect the inserted segment.");
        }

        return intersection;
    }

    static void Constrain(
        List<Edge> constrained,
        Edge edge,
        EdgeConstraintKind kind)
    {
        edge.Constrain(kind);
        constrained.Add(edge);
    }

    static void AddResult(
        TopologyChange change,
        HashSet<Face> affectedFaces,
        List<Edge> edgesToLegalize)
    {
        foreach (Face face in change.AffectedFaces)
        {
            affectedFaces.Add(face);
        }

        edgesToLegalize.AddRange(change.EdgesToLegalize);
    }
}
