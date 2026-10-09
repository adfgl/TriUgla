namespace TriUgla;

public sealed class EdgeInserter(
    IGeometry geometry,
    EdgeFlipper flipper,
    Splitter splitter,
    NodeFactory nodes)
{
    const int MaximumOperations = 100_000;
    readonly EdgeEntranceFinder _entrances = new(geometry);

    public bool SplitCrossedEdges { get; set; }

    public EdgeInsertResult Insert(
        Node start,
        Node end,
        EdgeConstraintKind kind = EdgeConstraintKind.Feature)
    {
        var insertion = new Insertion(start, end, kind);
        while (insertion.TryTake(out Node segmentStart, out Node segmentEnd))
        {
            InsertSegment(insertion, segmentStart, segmentEnd);
        }
        return insertion.Complete();
    }

    void InsertSegment(Insertion insertion, Node start, Node end)
    {
        Edge entrance = _entrances.Find(start, end)
            ?? throw new InvalidOperationException(
                $"Cannot find a face leaving node at {start.Position} toward {end.Position}.");

        if (ReferenceEquals(entrance.NodeEnd, end))
        {
            insertion.Constrain(entrance);
            return;
        }

        if (ContinuesAlongSegment(entrance, end))
        {
            insertion.Constrain(entrance);
            insertion.Enqueue(entrance.NodeEnd, end);
            return;
        }

        ResolveCrossing(insertion, start, end, entrance.Next);
    }

    void ResolveCrossing(Insertion insertion, Node start, Node end, Edge crossed)
    {
        if (CanRemoveByFlipping(crossed))
        {
            insertion.RecordTopologyChange(flipper.Flip(crossed).Change);
            insertion.Enqueue(start, end);
            return;
        }

        Node inserted = nodes.Create(
            FindIntersection(start, end, crossed),
            LocateResult.From(crossed));
        inserted.Kind = NodeKind.SteinerInsertion;
        insertion.RecordSplit(inserted, splitter.Split(crossed, inserted).Change);
        insertion.Enqueue(start, inserted);
        insertion.Enqueue(inserted, end);
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

    sealed class Insertion
    {
        readonly EdgeConstraintKind _kind;
        readonly SegmentQueue _segments = new();
        readonly List<Edge> _constrained = new(8);
        readonly List<Node> _inserted = [];
        readonly HashSet<Face> _affected = [];
        readonly List<Edge> _toLegalize = [];
        int _operations;

        public Insertion(Node start, Node end, EdgeConstraintKind kind)
        {
            _kind = kind;
            Enqueue(start, end);
        }

        public bool TryTake(out Node start, out Node end)
        {
            if (!_segments.TryDequeue(out start, out end)) return false;
            if (++_operations > MaximumOperations)
                throw new InvalidOperationException(
                    "Edge insertion did not converge. The mesh may contain invalid topology.");
            return true;
        }

        public void Enqueue(Node start, Node end) => _segments.TryEnqueue(start, end);

        public void Constrain(Edge edge)
        {
            edge.Constrain(_kind);
            _constrained.Add(edge);
        }

        public void RecordSplit(Node node, TopologyChange change)
        {
            _inserted.Add(node);
            RecordTopologyChange(change);
        }

        public void RecordTopologyChange(TopologyChange change)
        {
            foreach (Face face in change.AffectedFaces) _affected.Add(face);
            _toLegalize.AddRange(change.EdgesToLegalize);
        }

        public EdgeInsertResult Complete()
            => new(
                _constrained,
                _inserted,
                new TopologyChange(_affected.ToArray(), _toLegalize));
    }
}
