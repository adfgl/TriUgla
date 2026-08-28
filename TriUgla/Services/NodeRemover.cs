namespace TriUgla;

public sealed class NodeRemover : INodeRemover
{
    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!TryCollectCavity(node, out Cavity cavity) ||
            !EarClipper.TryTriangulate(cavity.Nodes, out var triangles))
        {
            return RemoveNodeResult.Failed(node);
        }

        Face[] affectedFaces = RebuildCavity(cavity, triangles);
        Face[] deadFaces = cavity.Faces.Skip(affectedFaces.Length).ToArray();
        Edge[] deadEdges = cavity.RadialEdges.ToArray();

        node.MarkDead();
        foreach (Face face in deadFaces)
        {
            face.MarkDead();
        }

        foreach (Edge edge in deadEdges)
        {
            edge.MarkDead();
        }

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

        var nodes = new List<Node>();
        var boundaryEdges = new List<Edge>();
        var faces = new List<Face>();
        var radialEdges = new HashSet<Edge>();
        var visited = new HashSet<Edge>();
        Edge start = node.Edge;
        Edge current = start;

        do
        {
            if (!visited.Add(current) ||
                current.Dead ||
                !ReferenceEquals(current.NodeStart, node) ||
                current.Twin is null ||
                current.OrTwinConstrained ||
                !IsTriangle(current))
            {
                return false;
            }

            Edge boundary = current.Next;
            Edge incoming = current.Prev;
            Edge? next = incoming.Twin;
            if (next is null || !ReferenceEquals(next.NodeStart, node))
            {
                return false;
            }

            nodes.Add(boundary.NodeStart);
            boundaryEdges.Add(boundary);
            faces.Add(current.Face);
            radialEdges.Add(current);
            radialEdges.Add(incoming);
            current = next;
        }
        while (!ReferenceEquals(current, start));

        if (nodes.Count < 3 || faces.Distinct().Count() != faces.Count)
        {
            return false;
        }

        cavity = new Cavity(nodes, boundaryEdges, faces, radialEdges);
        return true;
    }

    static Face[] RebuildCavity(
        Cavity cavity,
        IReadOnlyList<TriangleIndices> triangles)
    {
        var directedEdges = new Dictionary<(int Start, int End), Edge>();

        for (int index = 0; index < cavity.Nodes.Count; index++)
        {
            directedEdges.Add((index, (index + 1) % cavity.Nodes.Count), cavity.BoundaryEdges[index]);
        }

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
                cavity.Nodes[triangle.A],
                cavity.Nodes[triangle.B],
                cavity.Nodes[triangle.C]);
            affected[index] = face;
        }

        return affected;
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
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Edge> BoundaryEdges,
        IReadOnlyList<Face> Faces,
        IReadOnlySet<Edge> RadialEdges);
}
