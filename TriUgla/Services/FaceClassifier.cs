namespace TriUgla;

/// <summary>
/// Classifies connected face regions by their boundary nesting depth.
/// </summary>
public sealed class FaceClassifier
{
    readonly Mesh _mesh;
    readonly MeshTraversal _traversal;
    readonly SuperStructure _superStructure;
    readonly Queue<(Face Face, int Depth)> _regions;
    readonly Stack<Face> _stack;

    public FaceClassifier(
        Face root,
        MeshTraversal traversal,
        SuperStructure superStructure,
        int queueCapacity = 64,
        int stackCapacity = 256)
        : this(new Mesh(root), traversal, superStructure, queueCapacity, stackCapacity)
    {
    }

    public FaceClassifier(
        Mesh mesh,
        MeshTraversal traversal,
        SuperStructure superStructure,
        int queueCapacity = 64,
        int stackCapacity = 256)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        if (!ReferenceEquals(mesh.Root, traversal.Root))
            throw new ArgumentException("The root must match the traversal root.", nameof(mesh));
        _superStructure = superStructure ?? throw new ArgumentNullException(nameof(superStructure));
        _regions = new Queue<(Face, int)>(Math.Max(0, queueCapacity));
        _stack = new Stack<Face>(Math.Max(0, stackCapacity));
    }

    public Face Classify()
    {
        Face[] faces = _traversal.Faces().ToArray();
        foreach (Face face in faces) face.Kind = FaceKind.Undefined;

        _regions.Clear();
        _stack.Clear();
        foreach (Face face in faces.Where(_superStructure.SuperFace))
        {
            _regions.Enqueue((face, 0));
        }

        if (_regions.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot classify faces because the mesh has no face containing a super node.");
        }

        while (_regions.TryDequeue(out (Face Face, int Depth) region))
        {
            FloodRegion(region.Face, region.Depth);
        }

        Face? unclassified = faces.FirstOrDefault(face => face.Kind == FaceKind.Undefined);
        if (unclassified is not null)
        {
            throw new InvalidOperationException(
                "Cannot classify a face disconnected from the super structure.");
        }

        return _mesh.Root;
    }

    void FloodRegion(Face start, int depth)
    {
        FaceKind kind = KindAt(depth);
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
                    if (neighbour.Kind == FaceKind.Undefined)
                    {
                        _regions.Enqueue((neighbour, depth + 1));
                    }
                    else if (IsIsland(neighbour.Kind) == IsIsland(kind))
                    {
                        throw new InvalidOperationException(
                            "Boundary topology assigns the same parity to faces on both sides of an edge.");
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

    static FaceKind KindAt(int depth)
        => depth == 0
            ? FaceKind.Outside
            : depth % 2 == 1
                ? FaceKind.Island
                : FaceKind.Lake;

    static bool IsIsland(FaceKind kind) => kind == FaceKind.Island;

    static void EnsureKind(Face face, FaceKind expected)
    {
        if (face.Kind != expected)
        {
            throw new InvalidOperationException(
                $"Inconsistent boundary topology: face is both {face.Kind} and {expected}.");
        }
    }
}
