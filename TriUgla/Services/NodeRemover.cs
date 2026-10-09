namespace TriUgla;

public sealed class NodeRemover
{
    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!TryCollectCavity(node, out Cavity cavity)) return RemoveNodeResult.Failed(node);
        if (!EarClipper.TryTriangulate(cavity.BoundaryNodes, out var triangles))
            return RemoveNodeResult.Failed(node);

        Face[] affectedFaces = Retriangulate(cavity, triangles);
        Face[] deadFaces = [.. cavity.Faces[affectedFaces.Length..]];
        Edge[] deadEdges = [.. cavity.RadialEdges];
        Retire(node, deadFaces, deadEdges);
        Edge[] edgesToLegalize = CollectEdgesToLegalize(affectedFaces);

        return new RemoveNodeResult(
            true,
            node,
            new TopologyChange(affectedFaces, edgesToLegalize),
            deadFaces,
            deadEdges);
    }

    static bool TryCollectCavity(Node node, out Cavity cavity)
    {
        cavity = default;
        if (node.Dead || node.Constrained || node.Edge is null)
        {
            return false;
        }

        var boundaryNodes = new List<Node>();
        var boundaryEdges = new List<Edge>();
        var faces = new List<Face>();
        var radialEdges = new HashSet<Edge>();
        var visited = new HashSet<Edge>();
        Edge start = node.Edge;
        Edge current = start;

        do
        {
            if (!visited.Add(current) ||
                !TryAdvance(node, current, out Edge boundary, out Edge incoming, out Edge next))
                return false;

            boundaryNodes.Add(boundary.NodeStart);
            boundaryEdges.Add(boundary);
            faces.Add(current.Face);
            radialEdges.Add(current);
            radialEdges.Add(incoming);
            current = next;
        }
        while (!ReferenceEquals(current, start));

        if (boundaryNodes.Count < 3 || faces.Distinct().Count() != faces.Count) return false;

        cavity = new Cavity(
            boundaryNodes.ToArray(),
            boundaryEdges.ToArray(),
            faces.ToArray(),
            radialEdges.ToArray());
        return true;
    }

    static bool TryAdvance(
        Node center,
        Edge radial,
        out Edge boundary,
        out Edge incoming,
        out Edge next)
    {
        boundary = null!;
        incoming = null!;
        next = null!;
        if (radial.Dead ||
            !ReferenceEquals(radial.NodeStart, center) ||
            radial.Twin is null ||
            radial.OrTwinConstrained ||
            !IsTriangle(radial)) return false;

        boundary = radial.Next;
        incoming = radial.Prev;
        Edge? candidate = incoming.Twin;
        if (candidate is null || !ReferenceEquals(candidate.NodeStart, center)) return false;
        next = candidate;
        return true;
    }

    static Face[] Retriangulate(
        Cavity cavity,
        IReadOnlyList<TriangleIndices> triangles)
    {
        Dictionary<(int Start, int End), Edge> directedEdges = IndexBoundaryEdges(cavity);

        var affected = new Face[triangles.Count];
        for (int index = 0; index < triangles.Count; index++)
        {
            TriangleIndices triangle = triangles[index];
            Face face = cavity.Faces[index];
            Edge ab = GetOrCreateEdge(triangle.A, triangle.B, directedEdges);
            Edge bc = GetOrCreateEdge(triangle.B, triangle.C, directedEdges);
            Edge ca = GetOrCreateEdge(triangle.C, triangle.A, directedEdges);

            Linker.LinkTriangle(
                face,
                ab, bc, ca,
                cavity.BoundaryNodes[triangle.A],
                cavity.BoundaryNodes[triangle.B],
                cavity.BoundaryNodes[triangle.C]);
            affected[index] = face;
        }

        return affected;
    }

    static Dictionary<(int Start, int End), Edge> IndexBoundaryEdges(Cavity cavity)
    {
        var edges = new Dictionary<(int Start, int End), Edge>();
        for (int index = 0; index < cavity.BoundaryNodes.Length; index++)
            edges.Add((index, (index + 1) % cavity.BoundaryNodes.Length), cavity.BoundaryEdges[index]);
        return edges;
    }

    static void Retire(Node node, IEnumerable<Face> faces, IEnumerable<Edge> edges)
    {
        node.MarkDead();
        foreach (Face face in faces) face.MarkDead();
        foreach (Edge edge in edges) edge.MarkDead();
    }

    static Edge GetOrCreateEdge(
        int start,
        int end,
        Dictionary<(int Start, int End), Edge> directedEdges)
    {
        if (directedEdges.TryGetValue((start, end), out Edge? edge))
        {
            return edge;
        }

        edge = new Edge();
        directedEdges.Add((start, end), edge);

        if (directedEdges.TryGetValue((end, start), out Edge? twin))
        {
            Linker.LinkTwins(edge, twin);
        }

        return edge;
    }

    static Edge[] CollectEdgesToLegalize(IEnumerable<Face> faces)
    {
        var visited = new HashSet<Edge>();
        var result = new List<Edge>();

        foreach (Edge edge in faces.SelectMany(face => face.Edges))
        {
            if (!visited.Add(edge))
            {
                continue;
            }

            if (edge.Twin is not null)
            {
                visited.Add(edge.Twin);
            }

            result.Add(edge);
        }

        return result.ToArray();
    }

    static bool IsTriangle(Edge first)
        => ReferenceEquals(first.Next.Next, first.Prev) &&
           ReferenceEquals(first.Prev.Next, first);

    readonly record struct Cavity(
        Node[] BoundaryNodes,
        Edge[] BoundaryEdges,
        Face[] Faces,
        Edge[] RadialEdges);
}
