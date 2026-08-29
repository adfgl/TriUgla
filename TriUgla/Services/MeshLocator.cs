namespace TriUgla;

public sealed class MeshLocator : IMeshLocator
{
    readonly Mesh _mesh;
    readonly MeshTraversal _traversal;
    readonly StampSource _stamps;
    Face? _lastFound;

    public MeshLocator(Mesh mesh, MeshTraversal traversal, StampSource stamps)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
    }

    public double Eps { get; set; } = 1e-6;

    internal void Reset() => _lastFound = null;

    public LocateResult Locate(Vec2 point, Face? from = null)
    {
        ValidateEpsilon();

        Face current = from ?? _lastFound ?? _mesh.Root;
        Stamp stamp = NextStamp();
        current.TryVisit(stamp);

        while (true)
        {
            Edge nearestEdge = FindNearestEdge(current, point, out double minimumCross);
            LocateResult? result = Classify(current, nearestEdge, point, minimumCross);

            if (result is LocateResult found)
            {
                _lastFound = current;
                return found;
            }

            if (!TryMoveAcross(nearestEdge, stamp, out current))
            {
                _lastFound = null;
                return LocateResult.Empty;
            }
        }
    }

    LocateResult? Classify(Face face, Edge edge, Vec2 point, double minimumCross)
    {
        if (minimumCross < -Eps)
        {
            return null;
        }

        if (minimumCross > Eps)
        {
            return LocateResult.From(face);
        }

        if (IsNear(point, edge.NodeStart.Position))
        {
            return LocateResult.From(edge.NodeStart);
        }

        if (IsNear(point, edge.NodeEnd.Position))
        {
            return LocateResult.From(edge.NodeEnd);
        }

        return IsInsideEdgeBounds(point, edge) ? LocateResult.From(edge) : null;
    }

    bool IsNear(Vec2 first, Vec2 second)
        => first.DistanceSquared(second) <= Eps * Eps;

    bool IsInsideEdgeBounds(Vec2 point, Edge edge)
    {
        Vec2 min = Vec2.Min(edge.NodeStart.Position, edge.NodeEnd.Position);
        Vec2 max = Vec2.Max(edge.NodeStart.Position, edge.NodeEnd.Position);

        return point.X >= min.X - Eps && point.X <= max.X + Eps &&
               point.Y >= min.Y - Eps && point.Y <= max.Y + Eps;
    }

    static Edge FindNearestEdge(Face face, Vec2 point, out double minimumCross)
    {
        Edge nearest = face.Edge;
        minimumCross = double.PositiveInfinity;

        foreach (Edge edge in face.Edges)
        {
            Vec2 start = edge.NodeStart.Position;
            double cross = (edge.NodeEnd.Position - start).Cross(point - start);

            if (cross < minimumCross)
            {
                minimumCross = cross;
                nearest = edge;
            }
        }

        return nearest;
    }

    static bool TryMoveAcross(Edge edge, Stamp stamp, out Face next)
    {
        next = edge.Twin?.Face!;
        return next is not null && next.TryVisit(stamp);
    }

    void ValidateEpsilon()
    {
        if (double.IsNaN(Eps) || Eps < 0)
        {
            throw new InvalidOperationException("Eps must be a non-negative number.");
        }
    }

    Stamp NextStamp()
    {
        if (_stamps.TryNext(out Stamp stamp)) return stamp;
        _traversal.ResetVisitStamps();
        _stamps.Reset();
        _stamps.TryNext(out stamp);
        return stamp;
    }
}
