namespace TriUgla;

/// <summary>
/// Classifies connected face regions from directed loop boundaries.
/// Counterclockwise loops enclose solid regions and clockwise loops enclose holes.
/// </summary>
public sealed class FaceClassifier
{
    readonly Mesh _mesh;
    readonly SuperStructure _superStructure;
    readonly Queue<(Face Face, FaceKind Kind)> _regions;
    readonly Stack<Face> _stack;

    public FaceClassifier(
        Face root,
        SuperStructure superStructure,
        int queueCapacity = 64,
        int stackCapacity = 256)
        : this(new Mesh(root), superStructure, queueCapacity, stackCapacity)
    {
    }

    public FaceClassifier(
        Mesh mesh,
        SuperStructure superStructure,
        int queueCapacity = 64,
        int stackCapacity = 256)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        _superStructure = superStructure ?? throw new ArgumentNullException(nameof(superStructure));
        _regions = new Queue<(Face, FaceKind)>(Math.Max(0, queueCapacity));
        _stack = new Stack<Face>(Math.Max(0, stackCapacity));
    }

    public Face Classify()
    {
        Face[] faces = _mesh.Faces().ToArray();
        foreach (Face face in faces) face.Kind = FaceKind.Undefined;

        _regions.Clear();
        _stack.Clear();
        foreach (Face face in faces.Where(_superStructure.SuperFace))
        {
            _regions.Enqueue((face, FaceKind.Outside));
        }

        if (_regions.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot classify faces because the mesh has no face containing a super node.");
        }

        while (_regions.TryDequeue(out (Face Face, FaceKind Kind) region))
        {
            FloodRegion(region.Face, region.Kind);
        }

        Face? unclassified = faces.FirstOrDefault(face => face.Kind == FaceKind.Undefined);
        if (unclassified is not null)
        {
            throw new InvalidOperationException(
                "Cannot classify a face disconnected from the super structure.");
        }

        return _mesh.Root;
    }

    void FloodRegion(Face start, FaceKind kind)
    {
        if (start.Kind != FaceKind.Undefined)
        {
            EnsureKind(start, kind);
            return;
        }

        start.Kind = kind;
        _stack.Push(start);

        while (_stack.TryPop(out Face? face))
        {
            foreach (Edge edge in face.Edges)
            {
                Face? neighbour = edge.Twin?.Face;
                if (neighbour is null || neighbour.Dead) continue;

                if (HasBoundary(edge))
                {
                    int orientation = edge.Twin!.BoundaryConstraints -
                        edge.BoundaryConstraints;
                    if (orientation == 0)
                    {
                        throw new InvalidOperationException(
                            "A boundary edge is constrained equally in both directions.");
                    }
                    FaceKind neighbourKind = orientation > 0
                        ? FaceKind.Island
                        : FaceKind.Lake;
                    if (neighbour.Kind == FaceKind.Undefined)
                    {
                        _regions.Enqueue((neighbour, neighbourKind));
                    }
                    continue;
                }

                if (neighbour.Kind == FaceKind.Undefined)
                {
                    neighbour.Kind = kind;
                    _stack.Push(neighbour);
                }
                else
                {
                    EnsureKind(neighbour, kind);
                }
            }
        }
    }

    static bool HasBoundary(Edge edge)
        => edge.HasBoundary || edge.Twin?.HasBoundary == true;

    static void EnsureKind(Face face, FaceKind expected)
    {
        if (face.Kind != expected)
        {
            throw new InvalidOperationException(
                $"Inconsistent boundary topology: face is both {face.Kind} and {expected}.");
        }
    }
}
