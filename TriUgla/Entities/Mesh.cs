namespace TriUgla;

public delegate bool CanTraverseAcrossEdge(Face from, Edge border, Face to);

public sealed class Mesh
{
    Face _root;
    readonly StampSource _stamps = new();
    bool _requiresStampReset = true;

    public Mesh(Face root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public Face Root
    {
        get
        {
            if (!_root.Dead) return _root;

            _root = Faces(_root)
                .FirstOrDefault(face => !face.Dead)
                ?? throw new InvalidOperationException(
                    "The mesh root is dead and no reachable live face exists.");
            return _root;
        }
    }

    internal bool RootDead => _root.Dead;

    internal void SetRoot(Face root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public IEnumerable<Face> Faces(Face? from = null, CanTraverseAcrossEdge? canTraverse = null)
    {
        Face start = from ?? Root;
        if (_requiresStampReset)
        {
            ResetVisitStamps(_root);
            _requiresStampReset = false;
        }

        Stamp stamp = NextStamp();
        var stack = new Stack<Face>(256);
        if (!start.TryVisit(stamp))
        {
            ResetVisitStamps(start);
            if (!start.TryVisit(stamp)) yield break;
        }
        stack.Push(start);

        while (stack.TryPop(out Face? face))
        {
            yield return face;
            PushUnvisitedNeighbours(stack, face, stamp, canTraverse);
        }
    }

    public IEnumerable<Edge> Edges(Face? from = null, CanTraverseAcrossEdge? canTraverse = null)
    {
        foreach (Face face in Faces(from, canTraverse))
            foreach (Edge edge in face.Edges)
                yield return edge;
    }

    public IEnumerable<Node> Nodes(Face? from = null, CanTraverseAcrossEdge? canTraverse = null)
    {
        Stamp stamp = NextStamp();
        foreach (Edge edge in Edges(from, canTraverse))
        {
            if (edge.NodeStart.TryVisit(stamp)) yield return edge.NodeStart;
        }
    }

    public MeshSnapshot Snapshot(Face? from = null, CanTraverseAcrossEdge? canTraverse = null)
    {
        Stamp nodeStamp = NextStamp();
        var nodes = new List<Node>(256);
        var edges = new List<Edge>(512);
        var faces = new List<Face>(256);

        foreach (Face face in Faces(from, canTraverse))
        {
            faces.Add(face);
            foreach (Edge edge in face.Edges)
            {
                edges.Add(edge);
                if (edge.NodeStart.TryVisit(nodeStamp)) nodes.Add(edge.NodeStart);
            }
        }
        return new MeshSnapshot(nodes, edges, faces);
    }

    public void ResetVisitStamps()
        => ResetVisitStamps(Root);

    static void ResetVisitStamps(Face root)
    {
        var visitedFaces = new HashSet<Face>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<Face>(256);
        visitedFaces.Add(root);
        stack.Push(root);

        while (stack.TryPop(out Face? face))
        {
            face.ResetStamp();
            foreach (Edge edge in face.Edges)
            {
                edge.ResetStamp();
                edge.NodeStart.ResetStamp();
                Face? neighbour = edge.Twin?.Face;
                if (neighbour is not null && visitedFaces.Add(neighbour)) stack.Push(neighbour);
            }
        }
    }

    internal Stamp NextStamp()
    {
        if (_stamps.TryNext(out Stamp stamp)) return stamp;
        ResetVisitStamps(_root);
        _stamps.Reset();
        _stamps.TryNext(out stamp);
        return stamp;
    }

    static void PushUnvisitedNeighbours(
        Stack<Face> stack,
        Face face,
        Stamp stamp,
        CanTraverseAcrossEdge? canTraverse)
    {
        foreach (Edge edge in face.Edges)
        {
            Face? neighbour = edge.Twin?.Face;
            if (neighbour is null) continue;
            if (canTraverse is not null && !canTraverse(face, edge, neighbour)) continue;
            if (neighbour.TryVisit(stamp)) stack.Push(neighbour);
        }
    }

}
