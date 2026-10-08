namespace TriUgla;

/// <summary>
/// An immutable, index-based reconstruction of a triangular mesh. Its builder may
/// flip unconstrained edges in a private copy; the source mesh is never changed.
/// </summary>
public sealed class QuadMesh
{
    public IReadOnlyList<QuadMeshNode> Nodes { get; }
    public IReadOnlyList<QuadMeshFace> Quads { get; }
    public IReadOnlyList<QuadMeshTriangle> Triangles { get; }
    public IReadOnlyList<QuadMeshConstraint> Constraints { get; }
    public int EdgeFlips { get; }

    QuadMesh(QuadMeshNode[] nodes, QuadMeshFace[] quads,
        QuadMeshTriangle[] triangles, QuadMeshConstraint[] constraints, int edgeFlips)
    {
        Nodes = Array.AsReadOnly(nodes);
        Quads = Array.AsReadOnly(quads);
        Triangles = Array.AsReadOnly(triangles);
        Constraints = Array.AsReadOnly(constraints);
        EdgeFlips = edgeFlips;
    }

    public static QuadMesh From(Mesh mesh) => From(mesh, new QuadMeshSettings());

    public static QuadMesh From(Mesh mesh, in QuadMeshSettings settings)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (settings.MaxEdgeFlips < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "MaxEdgeFlips cannot be negative.");
        if (!double.IsFinite(settings.MinimumQuadQuality) ||
            settings.MinimumQuadQuality < 0d || settings.MinimumQuadQuality > 1d)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "MinimumQuadQuality must be between zero and one.");

        Face[] faces = CollectFaces(mesh.Root).Where(face => !face.Dead).ToArray();
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

        var working = new WorkingTriangle[faces.Length];
        for (int index = 0; index < faces.Length; index++)
        {
            Face face = faces[index];
            if (!IsTriangle(face))
                throw new InvalidOperationException("QuadMesh can only be built from triangular faces.");
            Edge edge = face.Edge;
            working[index] = new WorkingTriangle(
                Index(edge.NodeStart), Index(edge.Next.NodeStart), Index(edge.Prev.NodeStart), face.Kind);
        }

        QuadMeshNode[] nodes = sourceNodes
            .Select(node => new QuadMeshNode(node.Position, node.Data)).ToArray();
        BuildLocksAndConstraints(faces, indices, out HashSet<EdgeKey> locked,
            out QuadMeshConstraint[] constraints);

        Pairing pairing = Pair(working, nodes, locked, settings.MinimumQuadQuality);
        int flips = settings.AllowEdgeFlips
            ? ImproveByFlipping(working, nodes, locked, settings, ref pairing)
            : 0;

        QuadMeshFace[] quads = pairing.Candidates.Select(candidate =>
            new QuadMeshFace(candidate.Quad, working[candidate.First].Kind)).ToArray();
        var paired = new bool[working.Length];
        foreach (PairCandidate candidate in pairing.Candidates)
        {
            paired[candidate.First] = true;
            paired[candidate.Second] = true;
        }
        QuadMeshTriangle[] leftovers = working
            .Where((_, index) => !paired[index])
            .Select(t => new QuadMeshTriangle(new TriangleIndices(t.A, t.B, t.C), t.Kind))
            .ToArray();
        return new QuadMesh(nodes, quads, leftovers, constraints, flips);
    }

    static int ImproveByFlipping(WorkingTriangle[] triangles,
        IReadOnlyList<QuadMeshNode> nodes, HashSet<EdgeKey> locked,
        in QuadMeshSettings settings, ref Pairing pairing)
    {
        int flips = 0;
        while (flips < settings.MaxEdgeFlips)
        {
            Pairing? bestPairing = null;
            Flip? bestFlip = null;
            foreach ((EdgeKey edge, List<int> incident) in BuildAdjacency(triangles))
            {
                if (incident.Count != 2 || locked.Contains(edge)) continue;
                int first = incident[0];
                int second = incident[1];
                if (triangles[first].Kind != triangles[second].Kind ||
                    !TryBoundaryQuad(triangles[first], triangles[second], edge, nodes,
                        out QuadIndices boundary)) continue;

                // Rotate until the alternate diagonal is A-C rather than the current edge.
                while (edge.Contains(boundary.A)) boundary = Rotate(boundary);
                var replacement = new EdgeKey(boundary.A, boundary.C);
                if (locked.Contains(replacement)) continue;
                WorkingTriangle oldFirst = triangles[first];
                WorkingTriangle oldSecond = triangles[second];
                WorkingTriangle newFirst = new(boundary.A, boundary.B, boundary.C, oldFirst.Kind);
                WorkingTriangle newSecond = new(boundary.A, boundary.C, boundary.D, oldFirst.Kind);
                triangles[first] = newFirst;
                triangles[second] = newSecond;
                Pairing trial = Pair(triangles, nodes, locked, settings.MinimumQuadQuality);
                triangles[first] = oldFirst;
                triangles[second] = oldSecond;

                if (!Better(trial, pairing) ||
                    (bestPairing is not null && !Better(trial, bestPairing.Value))) continue;
                bestPairing = trial;
                bestFlip = new Flip(first, second, newFirst, newSecond);
            }

            if (bestFlip is null || bestPairing is null) break;
            Flip selected = bestFlip.Value;
            triangles[selected.First] = selected.FirstTriangle;
            triangles[selected.Second] = selected.SecondTriangle;
            pairing = bestPairing.Value;
            flips++;
        }
        return flips;
    }

    static QuadIndices Rotate(QuadIndices q) => new(q.B, q.C, q.D, q.A);

    static bool Better(Pairing candidate, Pairing current)
        => candidate.Candidates.Length > current.Candidates.Length ||
           candidate.Candidates.Length == current.Candidates.Length &&
           candidate.Quality > current.Quality + 1e-12;

    static Pairing Pair(WorkingTriangle[] triangles,
        IReadOnlyList<QuadMeshNode> nodes, HashSet<EdgeKey> locked, double minimumQuality)
    {
        var candidates = new List<PairCandidate>();
        foreach ((EdgeKey edge, List<int> incident) in BuildAdjacency(triangles))
        {
            if (incident.Count != 2 || locked.Contains(edge)) continue;
            int first = incident[0];
            int second = incident[1];
            if (triangles[first].Kind != triangles[second].Kind ||
                !TryBoundaryQuad(triangles[first], triangles[second], edge, nodes,
                    out QuadIndices quad)) continue;
            double quality = QuadScore(quad, nodes);
            if (quality >= minimumQuality)
                candidates.Add(new PairCandidate(first, second, quad, quality));
        }

        bool[] used = new bool[triangles.Length];
        PairCandidate[] selected = candidates
            .OrderByDescending(candidate => candidate.Quality)
            .ThenBy(candidate => candidate.First)
            .ThenBy(candidate => candidate.Second)
            .Where(candidate =>
            {
                if (used[candidate.First] || used[candidate.Second]) return false;
                used[candidate.First] = used[candidate.Second] = true;
                return true;
            }).ToArray();
        return new Pairing(selected, selected.Sum(candidate => candidate.Quality));
    }

    static Dictionary<EdgeKey, List<int>> BuildAdjacency(WorkingTriangle[] triangles)
    {
        var result = new Dictionary<EdgeKey, List<int>>();
        for (int index = 0; index < triangles.Length; index++)
        {
            WorkingTriangle triangle = triangles[index];
            Add(new EdgeKey(triangle.A, triangle.B), index);
            Add(new EdgeKey(triangle.B, triangle.C), index);
            Add(new EdgeKey(triangle.C, triangle.A), index);
        }
        return result;

        void Add(EdgeKey edge, int triangle)
        {
            if (!result.TryGetValue(edge, out List<int>? incident))
                result.Add(edge, incident = new List<int>(2));
            incident.Add(triangle);
        }
    }

    static bool TryBoundaryQuad(WorkingTriangle first, WorkingTriangle second,
        EdgeKey shared, IReadOnlyList<QuadMeshNode> nodes, out QuadIndices quad)
    {
        var boundary = new List<(int Start, int End)>(4);
        AddBoundary(first);
        AddBoundary(second);
        if (boundary.Count != 4) { quad = default; return false; }

        var ordered = new int[4];
        ordered[0] = boundary[0].Start;
        int current = boundary[0].End;
        for (int index = 1; index < 4; index++)
        {
            ordered[index] = current;
            int next = boundary.FindIndex(edge => edge.Start == current);
            if (next < 0) { quad = default; return false; }
            current = boundary[next].End;
        }
        if (current != ordered[0] || ordered.Distinct().Count() != 4)
        { quad = default; return false; }

        quad = new QuadIndices(ordered[0], ordered[1], ordered[2], ordered[3]);
        return IsConvex(quad, nodes);

        void AddBoundary(WorkingTriangle triangle)
        {
            Add(triangle.A, triangle.B);
            Add(triangle.B, triangle.C);
            Add(triangle.C, triangle.A);
        }
        void Add(int start, int end)
        {
            if (!new EdgeKey(start, end).Equals(shared)) boundary.Add((start, end));
        }
    }

    static bool IsConvex(QuadIndices quad, IReadOnlyList<QuadMeshNode> nodes)
    {
        Vec2[] p = [nodes[quad.A].Position, nodes[quad.B].Position,
                    nodes[quad.C].Position, nodes[quad.D].Position];
        double sign = 0d;
        for (int index = 0; index < 4; index++)
        {
            double cross = (p[(index + 1) % 4] - p[index])
                .Cross(p[(index + 2) % 4] - p[(index + 1) % 4]);
            if (!double.IsFinite(cross) || cross == 0d) return false;
            if (sign == 0d) sign = Math.Sign(cross);
            else if (Math.Sign(cross) != sign) return false;
        }
        return true;
    }

    static double QuadScore(QuadIndices quad, IReadOnlyList<QuadMeshNode> nodes)
    {
        Vec2[] p = [nodes[quad.A].Position, nodes[quad.B].Position,
                    nodes[quad.C].Position, nodes[quad.D].Position];
        double shortest = double.PositiveInfinity;
        double longest = 0d;
        double angles = 1d;
        for (int index = 0; index < 4; index++)
        {
            Vec2 incoming = p[(index + 3) % 4] - p[index];
            Vec2 outgoing = p[(index + 1) % 4] - p[index];
            double a = incoming.Length;
            double b = outgoing.Length;
            if (a == 0d || b == 0d) return 0d;
            shortest = Math.Min(shortest, b);
            longest = Math.Max(longest, b);
            angles = Math.Min(angles, 1d - Math.Abs(incoming.Dot(outgoing) / (a * b)));
        }
        return Math.Clamp(angles * shortest / longest, 0d, 1d);
    }

    static void BuildLocksAndConstraints(IReadOnlyList<Face> faces,
        IReadOnlyDictionary<Node, int> indices, out HashSet<EdgeKey> locked,
        out QuadMeshConstraint[] constraints)
    {
        locked = new HashSet<EdgeKey>();
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
        constraints = counts.Select(item => new QuadMeshConstraint(
            item.Key.A, item.Key.B, item.Value.Feature, item.Value.Boundary)).ToArray();
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

    readonly record struct EdgeKey
    {
        public int A { get; }
        public int B { get; }
        public EdgeKey(int first, int second)
        {
            A = Math.Min(first, second);
            B = Math.Max(first, second);
        }
        public bool Contains(int node) => A == node || B == node;
    }

    readonly record struct WorkingTriangle(int A, int B, int C, FaceKind Kind);
    readonly record struct PairCandidate(int First, int Second, QuadIndices Quad, double Quality);
    readonly record struct Pairing(PairCandidate[] Candidates, double Quality);
    readonly record struct Flip(int First, int Second,
        WorkingTriangle FirstTriangle, WorkingTriangle SecondTriangle);
}
