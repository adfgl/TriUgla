namespace TriUgla;

/// <summary>Builds a detached quad-dominant mesh without modifying its source.</summary>
public sealed class QuadMeshBuilder
{
    readonly GeometryPredicates _geometry = new();

    public QuadMesh Build(Mesh source) => Build(source, new QuadMeshSettings());

    public QuadMesh Build(Mesh source, in QuadMeshSettings settings)
    {
        ArgumentNullException.ThrowIfNull(source);
        Validate(settings);
        Mesh copy = Clone(source);
        int flips = settings.AllowEdgeFlips
            ? ImprovePairing(copy, settings.MinimumQuadQuality, settings.MaxEdgeFlips) : 0;
        Pairing pairing = FindPairing(copy, settings.MinimumQuadQuality);
        MergePairs(copy, pairing.Pairs);
        return Export(copy, flips);
    }

    int ImprovePairing(Mesh mesh, double minimumQuality, int maximumFlips)
    {
        int accepted = 0;
        Pairing current = FindPairing(mesh, minimumQuality);
        var flipper = new EdgeFlipper(_geometry);
        while (accepted < maximumFlips)
        {
            bool improved = false;
            Edge[] edges = CollectInternalEdges(mesh);
            for (int index = 0; index < edges.Length; index++)
            {
                Edge edge = edges[index];
                if (!CanFlip(edge)) continue;
                flipper.Flip(edge);
                Pairing candidate = FindPairing(mesh, minimumQuality);
                if (candidate.BetterThan(current))
                {
                    current = candidate;
                    accepted++;
                    improved = true;
                    break;
                }
                flipper.Flip(edge);
            }
            if (!improved) break;
        }
        return accepted;
    }

    bool CanFlip(Edge edge)
    {
        Edge? twin = edge.Twin;
        return !edge.Dead && twin is not null && !twin.Dead && !edge.OrTwinConstrained &&
            edge.Face.Kind == twin.Face.Kind && IsTriangle(edge.Face) && IsTriangle(twin.Face) &&
            _geometry.IsConvexQuad(Quad.From(edge));
    }

    Pairing FindPairing(Mesh mesh, double minimumQuality)
    {
        Face[] faces = LiveFaces(mesh);
        Dictionary<Face, int> depths = BoundaryDepths(faces);
        var candidates = new List<PairCandidate>(faces.Length * 2);
        var visited = new HashSet<Edge>(ReferenceEqualityComparer.Instance);
        for (int faceIndex = 0; faceIndex < faces.Length; faceIndex++)
        {
            foreach (Edge edge in faces[faceIndex].Edges)
            {
                if (!visited.Add(edge)) continue;
                if (edge.Twin is Edge twin) visited.Add(twin);
                if (TryCandidate(edge, depths, minimumQuality, out PairCandidate candidate))
                    candidates.Add(candidate);
            }
        }
        candidates.Sort(static (first, second) =>
        {
            int depth = first.Depth.CompareTo(second.Depth);
            if (depth != 0) return depth;
            int boundary = second.TouchesBoundary.CompareTo(first.TouchesBoundary);
            return boundary != 0 ? boundary : second.Quality.CompareTo(first.Quality);
        });

        var paired = new HashSet<Face>(ReferenceEqualityComparer.Instance);
        var pairs = new List<PairCandidate>(faces.Length / 2);
        double quality = 0d;
        for (int index = 0; index < candidates.Count; index++)
        {
            PairCandidate candidate = candidates[index];
            if (!paired.Add(candidate.First)) continue;
            if (!paired.Add(candidate.Second))
            {
                paired.Remove(candidate.First);
                continue;
            }
            pairs.Add(candidate);
            quality += candidate.Quality;
        }
        return new Pairing(pairs.ToArray(), quality);
    }

    bool TryCandidate(Edge edge, Dictionary<Face, int> depths, double minimumQuality,
        out PairCandidate candidate)
    {
        candidate = default;
        Edge? twin = edge.Twin;
        if (twin is null || edge.Dead || twin.Dead || edge.OrTwinConstrained ||
            edge.Face.Dead || twin.Face.Dead || edge.Face.Kind != twin.Face.Kind ||
            !IsTriangle(edge.Face) || !IsTriangle(twin.Face)) return false;
        Quad quad = Quad.From(edge);
        if (!_geometry.IsConvexQuad(quad)) return false;
        double quality = Quality(quad);
        if (quality < minimumQuality) return false;
        candidate = new PairCandidate(edge, edge.Face, twin.Face, quality,
            Math.Min(depths[edge.Face], depths[twin.Face]),
            TouchesBoundary(edge) || TouchesBoundary(twin));
        return true;
    }

    static bool TouchesBoundary(Edge diagonal)
        => diagonal.Next.Twin is null || diagonal.Prev.Twin is null;

    static Dictionary<Face, int> BoundaryDepths(Face[] faces)
    {
        var depths = new Dictionary<Face, int>(faces.Length, ReferenceEqualityComparer.Instance);
        var queue = new Queue<Face>(faces.Length);
        for (int index = 0; index < faces.Length; index++)
        {
            Face face = faces[index];
            if (!TouchesBoundary(face)) continue;
            depths.Add(face, 0);
            queue.Enqueue(face);
        }
        while (queue.TryDequeue(out Face? face))
        {
            int nextDepth = depths[face] + 1;
            foreach (Edge edge in face.Edges)
            {
                Face? neighbour = edge.Twin?.Face;
                if (neighbour is null || neighbour.Dead || depths.ContainsKey(neighbour)) continue;
                depths.Add(neighbour, nextDepth);
                queue.Enqueue(neighbour);
            }
        }
        for (int index = 0; index < faces.Length; index++)
            if (!depths.ContainsKey(faces[index])) depths.Add(faces[index], int.MaxValue);
        return depths;
    }

    static bool TouchesBoundary(Face face)
    {
        foreach (Edge edge in face.Edges)
            if (edge.Twin is null) return true;
        return false;
    }

    static void MergePairs(Mesh mesh, PairCandidate[] pairs)
    {
        for (int index = 0; index < pairs.Length; index++)
        {
            Edge diagonal = pairs[index].Diagonal;
            Edge twin = diagonal.Twin!;
            Face kept = diagonal.Face;
            Face removed = twin.Face;
            Edge ab = twin.Next;
            Edge bc = twin.Prev;
            Edge cd = diagonal.Next;
            Edge da = diagonal.Prev;
            Linker.LinkEdges(ab, bc);
            Linker.LinkEdges(bc, cd);
            Linker.LinkEdges(cd, da);
            Linker.LinkEdges(da, ab);
            kept.Edge = ab;
            ab.Face = bc.Face = cd.Face = da.Face = kept;
            ab.NodeStart.Edge = ab;
            bc.NodeStart.Edge = bc;
            cd.NodeStart.Edge = cd;
            da.NodeStart.Edge = da;
            diagonal.MarkDead();
            twin.MarkDead();
            removed.MarkDead();
            if (ReferenceEquals(mesh.Root, removed)) mesh.SetRoot(kept);
        }
    }

    static Mesh Clone(Mesh source)
    {
        Face[] sourceFaces = LiveFaces(source);
        var nodes = new Dictionary<Node, Node>(ReferenceEqualityComparer.Instance);
        var faces = new Dictionary<Face, Face>(ReferenceEqualityComparer.Instance);
        var edges = new Dictionary<Edge, Edge>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < sourceFaces.Length; index++)
        {
            Face sourceFace = sourceFaces[index];
            faces.Add(sourceFace, new Face { Kind = sourceFace.Kind });
            foreach (Edge sourceEdge in sourceFace.Edges)
            {
                if (!nodes.ContainsKey(sourceEdge.NodeStart))
                {
                    Node sourceNode = sourceEdge.NodeStart;
                    var copyNode = new Node
                    {
                        Position = sourceNode.Position,
                        Data = sourceNode.Data,
                        Kind = sourceNode.Kind
                    };
                    copyNode.CopyConstraintState(sourceNode);
                    nodes.Add(sourceNode, copyNode);
                }
                edges.Add(sourceEdge, new Edge());
            }
        }
        foreach (KeyValuePair<Edge, Edge> item in edges)
        {
            Edge sourceEdge = item.Key;
            Edge copy = item.Value;
            copy.NodeStart = nodes[sourceEdge.NodeStart];
            if (copy.NodeStart.Edge is null) copy.NodeStart.Edge = copy;
            copy.Next = edges[sourceEdge.Next];
            copy.Prev = edges[sourceEdge.Prev];
            copy.Face = faces[sourceEdge.Face];
            if (sourceEdge.Twin is Edge sourceTwin && edges.TryGetValue(sourceTwin, out Edge? twin))
                copy.Twin = twin;
        }
        foreach (KeyValuePair<Face, Face> item in faces) item.Value.Edge = edges[item.Key.Edge];
        foreach (KeyValuePair<Edge, Edge> item in edges)
            item.Value.CopyConstraintState(item.Key);
        return new Mesh(faces[source.Root]);
    }

    static QuadMesh Export(Mesh mesh, int flips)
    {
        Face[] faces = LiveFaces(mesh);
        var nodeIndices = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
        var nodes = new List<QuadMeshNode>();
        var quads = new List<QuadMeshFace>();
        var triangles = new List<QuadMeshTriangle>();
        var constraints = new Dictionary<(int A, int B), (int Features, int Boundaries)>();
        int Index(Node node)
        {
            if (nodeIndices.TryGetValue(node, out int value)) return value;
            value = nodes.Count;
            nodeIndices.Add(node, value);
            nodes.Add(new QuadMeshNode(node.Position, node.Data, node.Kind));
            return value;
        }
        for (int faceIndex = 0; faceIndex < faces.Length; faceIndex++)
        {
            Face face = faces[faceIndex];
            var indices = new List<int>(4);
            foreach (Edge edge in face.Edges)
            {
                int start = Index(edge.NodeStart);
                int end = Index(edge.NodeEnd);
                indices.Add(start);
                if (edge.ConstraintCount == 0) continue;
                (int A, int B) key = start < end ? (start, end) : (end, start);
                constraints.TryGetValue(key, out var counts);
                constraints[key] = (counts.Features + edge.FeatureConstraints,
                    counts.Boundaries + edge.BoundaryConstraints);
            }
            if (indices.Count == 4)
                quads.Add(new QuadMeshFace(
                    new QuadIndices(indices[0], indices[1], indices[2], indices[3]), face.Kind));
            else if (indices.Count == 3)
                triangles.Add(new QuadMeshTriangle(
                    new TriangleIndices(indices[0], indices[1], indices[2]), face.Kind));
            else
                throw new InvalidOperationException("Quad reconstruction produced an invalid face cycle.");
        }
        var exportedConstraints = new QuadMeshConstraint[constraints.Count];
        int constraintIndex = 0;
        foreach (KeyValuePair<(int A, int B), (int Features, int Boundaries)> item in constraints)
            exportedConstraints[constraintIndex++] = new QuadMeshConstraint(
                item.Key.A, item.Key.B, item.Value.Features, item.Value.Boundaries);
        return new QuadMesh(mesh, nodes.ToArray(), quads.ToArray(), triangles.ToArray(),
            exportedConstraints, flips);
    }

    static Face[] LiveFaces(Mesh mesh)
    {
        var result = new List<Face>();
        foreach (Face face in mesh.Faces())
            if (!face.Dead) result.Add(face);
        return result.ToArray();
    }

    static Edge[] CollectInternalEdges(Mesh mesh)
    {
        var result = new List<Edge>();
        var visited = new HashSet<Edge>(ReferenceEqualityComparer.Instance);
        foreach (Face face in mesh.Faces())
        foreach (Edge edge in face.Edges)
        {
            if (!visited.Add(edge)) continue;
            if (edge.Twin is not Edge twin) continue;
            visited.Add(twin);
            result.Add(edge);
        }
        return result.ToArray();
    }

    static bool IsTriangle(Face face)
    {
        Edge first = face.Edge;
        Edge second = first.Next;
        Edge third = first.Prev;
        return ReferenceEquals(second.Next, third) && ReferenceEquals(third.Next, first);
    }

    static double Quality(Quad quad)
    {
        Vec2[] points = [quad.A.Position, quad.B.Position, quad.C.Position, quad.D.Position];
        double shortest = double.PositiveInfinity;
        double longest = 0d;
        double angleQuality = 1d;
        for (int index = 0; index < 4; index++)
        {
            Vec2 previous = points[(index + 3) % 4] - points[index];
            Vec2 next = points[(index + 1) % 4] - points[index];
            double length = next.Length;
            shortest = Math.Min(shortest, length);
            longest = Math.Max(longest, length);
            double denominator = previous.Length * length;
            if (denominator == 0d) return 0d;
            angleQuality = Math.Min(angleQuality, 1d - Math.Abs(previous.Dot(next) / denominator));
        }
        return longest == 0d ? 0d : Math.Clamp(angleQuality * shortest / longest, 0d, 1d);
    }

    static void Validate(in QuadMeshSettings settings)
    {
        if (settings.MaxEdgeFlips < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "MaxEdgeFlips cannot be negative.");
        if (!double.IsFinite(settings.MinimumQuadQuality) ||
            settings.MinimumQuadQuality < 0d || settings.MinimumQuadQuality > 1d)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "MinimumQuadQuality must be between zero and one.");
    }

    readonly record struct PairCandidate(Edge Diagonal, Face First, Face Second,
        double Quality, int Depth, bool TouchesBoundary);

    readonly record struct Pairing(PairCandidate[] Pairs, double Quality)
    {
        public bool BetterThan(Pairing other)
            => Pairs.Length > other.Pairs.Length ||
               Pairs.Length == other.Pairs.Length && Quality > other.Quality + 1e-12;
    }
}
