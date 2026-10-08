namespace TriUgla;

internal readonly record struct ReconstructionTriangle(int A, int B, int C, FaceKind Kind)
{
    public void AddEdges(HashSet<EdgeKey> edges)
    {
        edges.Add(new EdgeKey(A, B));
        edges.Add(new EdgeKey(B, C));
        edges.Add(new EdgeKey(C, A));
    }
}

internal readonly record struct EdgeKey
{
    public int A { get; }
    public int B { get; }

    public EdgeKey(int first, int second)
    {
        A = Math.Min(first, second);
        B = Math.Max(first, second);
    }
}

internal readonly record struct EdgeAdjacency(int First, int Second = -1)
{
    public bool Internal => Second >= 0;

    public EdgeAdjacency Add(int triangle)
    {
        if (First == triangle || Second == triangle) return this;
        if (Second >= 0) throw new InvalidOperationException("The source mesh is non-manifold.");
        return new EdgeAdjacency(First, triangle);
    }

    public EdgeAdjacency Remove(int triangle)
    {
        if (Second == triangle) return new EdgeAdjacency(First);
        if (First == triangle && Second >= 0) return new EdgeAdjacency(Second);
        if (First == triangle) return new EdgeAdjacency(-1);
        return this;
    }
}

internal sealed class ReconstructionMesh
{
    readonly Dictionary<EdgeKey, EdgeAdjacency> _adjacency;
    readonly HashSet<EdgeKey> _locked;

    public QuadMeshNode[] Nodes { get; }
    public ReconstructionTriangle[] Triangles { get; }
    public QuadMeshConstraint[] Constraints { get; }
    public IEnumerable<KeyValuePair<EdgeKey, EdgeAdjacency>> Edges => _adjacency;

    ReconstructionMesh(QuadMeshNode[] nodes, ReconstructionTriangle[] triangles,
        QuadMeshConstraint[] constraints, HashSet<EdgeKey> locked)
    {
        Nodes = nodes;
        Triangles = triangles;
        Constraints = constraints;
        _locked = locked;
        _adjacency = new Dictionary<EdgeKey, EdgeAdjacency>(triangles.Length * 2);
        for (int index = 0; index < triangles.Length; index++) AddTriangle(index, triangles[index]);
    }

    public bool Locked(EdgeKey edge) => _locked.Contains(edge);

    public bool TryGetAdjacency(EdgeKey edge, out EdgeAdjacency adjacency)
        => _adjacency.TryGetValue(edge, out adjacency);

    public HashSet<int> Neighbourhood(int first, int second)
    {
        var result = new HashSet<int> { first, second };
        AddNeighbours(first);
        AddNeighbours(second);
        return result;

        void AddNeighbours(int triangleIndex)
        {
            var edges = new HashSet<EdgeKey>();
            Triangles[triangleIndex].AddEdges(edges);
            foreach (EdgeKey edge in edges)
            {
                EdgeAdjacency adjacent = _adjacency[edge];
                if (adjacent.First >= 0) result.Add(adjacent.First);
                if (adjacent.Second >= 0) result.Add(adjacent.Second);
            }
        }
    }

    public void Replace(int first, ReconstructionTriangle firstTriangle,
        int second, ReconstructionTriangle secondTriangle)
    {
        RemoveTriangle(first, Triangles[first]);
        RemoveTriangle(second, Triangles[second]);
        Triangles[first] = firstTriangle;
        Triangles[second] = secondTriangle;
        AddTriangle(first, firstTriangle);
        AddTriangle(second, secondTriangle);
    }

    void AddTriangle(int index, ReconstructionTriangle triangle)
    {
        Add(new EdgeKey(triangle.A, triangle.B));
        Add(new EdgeKey(triangle.B, triangle.C));
        Add(new EdgeKey(triangle.C, triangle.A));

        void Add(EdgeKey edge)
        {
            if (_adjacency.TryGetValue(edge, out EdgeAdjacency adjacent))
                _adjacency[edge] = adjacent.Add(index);
            else
                _adjacency.Add(edge, new EdgeAdjacency(index));
        }
    }

    void RemoveTriangle(int index, ReconstructionTriangle triangle)
    {
        Remove(new EdgeKey(triangle.A, triangle.B));
        Remove(new EdgeKey(triangle.B, triangle.C));
        Remove(new EdgeKey(triangle.C, triangle.A));

        void Remove(EdgeKey edge)
        {
            EdgeAdjacency adjacent = _adjacency[edge].Remove(index);
            if (adjacent.First < 0) _adjacency.Remove(edge);
            else _adjacency[edge] = adjacent;
        }
    }

    public static ReconstructionMesh From(Mesh source)
    {
        Face[] faces = CollectFaces(source.Root).Where(face => !face.Dead).ToArray();
        var sourceNodes = new List<Node>();
        var indices = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
        int Index(Node node)
        {
            if (indices.TryGetValue(node, out int index)) return index;
            index = sourceNodes.Count;
            sourceNodes.Add(node);
            indices.Add(node, index);
            return index;
        }

        var triangles = new ReconstructionTriangle[faces.Length];
        for (int index = 0; index < faces.Length; index++)
        {
            Face face = faces[index];
            if (!IsTriangle(face))
                throw new InvalidOperationException("QuadMesh can only be built from triangular faces.");
            Edge edge = face.Edge;
            triangles[index] = new ReconstructionTriangle(
                Index(edge.NodeStart), Index(edge.Next.NodeStart), Index(edge.Prev.NodeStart), face.Kind);
        }

        var locked = new HashSet<EdgeKey>();
        var counts = new Dictionary<EdgeKey, (int Feature, int Boundary)>();
        foreach (Face face in faces)
        foreach (Edge edge in face.Edges)
        {
            EdgeKey key = new(indices[edge.NodeStart], indices[edge.NodeEnd]);
            Face? neighbour = edge.Twin?.Face;
            if (edge.Twin is null || edge.OrTwinConstrained || neighbour is null ||
                neighbour.Dead || neighbour.Kind != face.Kind) locked.Add(key);
            if (edge.ConstraintCount == 0) continue;
            counts.TryGetValue(key, out (int Feature, int Boundary) count);
            counts[key] = (count.Feature + edge.FeatureConstraints,
                count.Boundary + edge.BoundaryConstraints);
        }

        QuadMeshNode[] nodes = sourceNodes
            .Select(node => new QuadMeshNode(node.Position, node.Data, node.Kind)).ToArray();
        QuadMeshConstraint[] constraints = counts.Select(item => new QuadMeshConstraint(
            item.Key.A, item.Key.B, item.Value.Feature, item.Value.Boundary)).ToArray();
        return new ReconstructionMesh(nodes, triangles, constraints, locked);
    }

    static IEnumerable<Face> CollectFaces(Face root)
    {
        var visited = new HashSet<Face>(ReferenceEqualityComparer.Instance) { root };
        var stack = new Stack<Face>();
        stack.Push(root);
        while (stack.TryPop(out Face? face))
        {
            yield return face;
            foreach (Edge edge in face.Edges)
            {
                Face? neighbour = edge.Twin?.Face;
                if (neighbour is not null && visited.Add(neighbour)) stack.Push(neighbour);
            }
        }
    }

    static bool IsTriangle(Face face)
    {
        Edge first = face.Edge;
        if (first is null || first.Next is null || first.Prev is null) return false;
        Edge second = first.Next;
        Edge third = first.Prev;
        return ReferenceEquals(second.Next, third) && ReferenceEquals(third.Next, first) &&
            ReferenceEquals(second.Prev, first) && ReferenceEquals(third.Prev, second);
    }
}
