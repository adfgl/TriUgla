namespace TriUgla;

public sealed class Mesh
{
    Face _root;

    public Mesh(Face root)
        => _root = root ?? throw new ArgumentNullException(nameof(root));

    public Face Root
    {
        get
        {
            if (!_root.Dead) return _root;

            Face? replacement = FindLiveFace(_root);
            _root = replacement ?? throw new InvalidOperationException(
                "The mesh root is dead and no reachable live face exists.");
            return _root;
        }
    }

    internal void SetRoot(Face root)
        => _root = root ?? throw new ArgumentNullException(nameof(root));

    static Face? FindLiveFace(Face start)
    {
        var visited = new HashSet<Face>(ReferenceEqualityComparer.Instance) { start };
        var stack = new Stack<Face>();
        stack.Push(start);

        while (stack.TryPop(out Face? face))
        {
            if (!face.Dead) return face;

            foreach (Edge edge in face.Edges)
            {
                Face? neighbour = edge.Twin?.Face;
                if (neighbour is not null && visited.Add(neighbour))
                {
                    stack.Push(neighbour);
                }
            }
        }
        return null;
    }
}
