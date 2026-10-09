namespace TriUgla;

public sealed class NodeRemover(IGeometry? geometry = null)
{
    readonly IGeometry _geometry = geometry ?? new GeometryPredicates();

    public RemoveNodeResult Remove(Node node, out ConstrainedRemoval? constrained)
    {
        ArgumentNullException.ThrowIfNull(node);
        constrained = null;
        if (!TryPlan(node, out RemovalPlan plan))
            return RemoveNodeResult.Failed(node);

        Prepare(plan);
        if (!TryTriangulate(plan, out Triangulation triangulation))
        {
            RollBack(plan);
            return RemoveNodeResult.Failed(node);
        }

        constrained = plan.Constraint;
        return Commit(plan, triangulation);
    }

    public RemoveNodeResult Remove(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return Remove(node, out _);
    }

    bool TryPlan(Node node, out RemovalPlan plan)
    {
        plan = default;
        if (node.Dead || node.Kind == NodeKind.Super) return false;

        ConstrainedSpoke[] spokes = CollectConstrainedSpokes(node);
        ConstrainedRemoval? constraint = null;
        if (spokes.Length != 0)
        {
            if (!TryPlanConstraint(node, spokes, out ConstrainedRemoval removal)) return false;
            constraint = removal;
        }
        else if (node.Kind == NodeKind.SteinerInsertion)
        {
            return false;
        }

        plan = new RemovalPlan(node, constraint);
        return true;
    }

    bool TryPlanConstraint(
        Node node,
        ConstrainedSpoke[] spokes,
        out ConstrainedRemoval removal)
    {
        removal = default;
        if (node.Kind is not (NodeKind.SteinerRefinement or NodeKind.SteinerInsertion) ||
            spokes.Length != 2)
            return false;

        ConstrainedSpoke first = spokes[0];
        ConstrainedSpoke second = spokes[1];
        ConstraintCounts forward = CountConstraints(first, first.Other, node);
        ConstraintCounts reverse = CountConstraints(first, node, first.Other);
        if (forward != CountConstraints(second, node, second.Other) ||
            reverse != CountConstraints(second, second.Other, node) ||
            _geometry.Orient(first.Other, second.Other, node.Position) != EOrientaiton.Collinear ||
            (first.Other.Position - node.Position).Dot(second.Other.Position - node.Position) >= 0d)
            return false;

        EdgeConstraintState[] states = CaptureConstraintStates(first, second);
        removal = new ConstrainedRemoval(
            first.Other, second.Other, forward, reverse, states);
        return true;
    }

    static void Prepare(RemovalPlan plan)
    {
        if (plan.Constraint is ConstrainedRemoval constraint)
            Release(constraint.Edges);
    }

    static bool TryTriangulate(RemovalPlan plan, out Triangulation triangulation)
    {
        triangulation = default;
        if (!TryCollectCavity(plan.Node, out Cavity cavity) ||
            !EarClipper.TryTriangulate(cavity.BoundaryNodes, out var triangles))
            return false;

        triangulation = new Triangulation(cavity, triangles);
        return true;
    }

    static void RollBack(RemovalPlan plan)
    {
        if (plan.Constraint is ConstrainedRemoval constraint)
            Restore(constraint.Edges);
    }

    static RemoveNodeResult Commit(RemovalPlan plan, Triangulation triangulation)
    {
        Cavity cavity = triangulation.Cavity;
        Node node = plan.Node;

        Face[] affectedFaces = Retriangulate(cavity, triangulation.Triangles);
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
        if (node.Dead || node.Kind == NodeKind.Super || node.Constrained || node.Edge is null)
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

        if (boundaryNodes.Count < 3 || ContainsDuplicateFaces(faces)) return false;

        cavity = new Cavity(
            boundaryNodes.ToArray(),
            boundaryEdges.ToArray(),
            faces.ToArray(),
            radialEdges.ToArray());
        return true;
    }

    static bool ContainsDuplicateFaces(List<Face> faces)
    {
        var unique = new HashSet<Face>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < faces.Count; index++)
        {
            if (!unique.Add(faces[index])) return true;
        }
        return false;
    }

    static ConstrainedSpoke[] CollectConstrainedSpokes(Node node)
    {
        var groups = new Dictionary<Node, HashSet<Edge>>(ReferenceEqualityComparer.Instance);
        foreach (Edge outgoing in IncidentEdges(node))
        {
            Add(outgoing);
            if (outgoing.Twin is not null) Add(outgoing.Twin);
        }
        var spokes = new ConstrainedSpoke[groups.Count];
        int index = 0;
        foreach (KeyValuePair<Node, HashSet<Edge>> group in groups)
            spokes[index++] = new ConstrainedSpoke(group.Key, CopyToArray(group.Value));
        return spokes;

        void Add(Edge edge)
        {
            if (edge.Dead || !edge.Constrained || !edge.Contains(node)) return;
            Node other = ReferenceEquals(edge.NodeStart, node) ? edge.NodeEnd : edge.NodeStart;
            if (!groups.TryGetValue(other, out HashSet<Edge>? halfEdges))
                groups.Add(other, halfEdges = new HashSet<Edge>(ReferenceEqualityComparer.Instance));
            halfEdges.Add(edge);
        }
    }

    static IEnumerable<Edge> IncidentEdges(Node node)
    {
        Edge first = node.Edge;
        if (first is null) yield break;
        Edge current = first;
        bool closed = false;
        do
        {
            yield return current;
            Edge? next = current.Prev.Twin;
            if (next is null) break;
            current = next;
            closed = ReferenceEquals(current, first);
        }
        while (!closed);
        if (closed) yield break;

        current = first;
        while (current.Twin is not null)
        {
            current = current.Twin.Next;
            if (ReferenceEquals(current, first)) yield break;
            yield return current;
        }
    }

    static ConstraintCounts CountConstraints(ConstrainedSpoke spoke, Node from, Node to)
    {
        int features = 0;
        int boundaries = 0;
        for (int index = 0; index < spoke.HalfEdges.Length; index++)
        {
            Edge edge = spoke.HalfEdges[index];
            if (!ReferenceEquals(edge.NodeStart, from) || !ReferenceEquals(edge.NodeEnd, to))
                continue;
            features += edge.FeatureConstraints;
            boundaries += edge.BoundaryConstraints;
        }
        return new ConstraintCounts(features, boundaries);
    }

    static EdgeConstraintState[] CaptureConstraintStates(
        ConstrainedSpoke first,
        ConstrainedSpoke second)
    {
        var states = new EdgeConstraintState[first.HalfEdges.Length + second.HalfEdges.Length];
        int count = Capture(first.HalfEdges, states, 0);
        Capture(second.HalfEdges, states, count);
        return states;
    }

    static int Capture(Edge[] edges, EdgeConstraintState[] states, int offset)
    {
        for (int index = 0; index < edges.Length; index++)
        {
            Edge edge = edges[index];
            states[offset + index] = new EdgeConstraintState(
                edge, edge.FeatureConstraints, edge.BoundaryConstraints);
        }
        return offset + edges.Length;
    }

    static T[] CopyToArray<T>(HashSet<T> source)
    {
        var result = new T[source.Count];
        source.CopyTo(result);
        return result;
    }

    static void Release(IEnumerable<EdgeConstraintState> states)
    {
        foreach (EdgeConstraintState state in states)
        {
            Edge edge = state.Edge;
            while (edge.HasFeature) edge.Release(EdgeConstraintKind.Feature);
            while (edge.HasBoundary) edge.Release(EdgeConstraintKind.Boundary);
        }
    }

    static void Restore(IEnumerable<EdgeConstraintState> states)
    {
        foreach (EdgeConstraintState state in states)
        {
            for (int index = 0; index < state.Features; index++)
                state.Edge.Constrain(EdgeConstraintKind.Feature);
            for (int index = 0; index < state.Boundaries; index++)
                state.Edge.Constrain(EdgeConstraintKind.Boundary);
        }
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

        foreach (Face face in faces)
        {
            foreach (Edge edge in face.Edges)
            {
                if (!visited.Add(edge)) continue;

                if (edge.Twin is not null) visited.Add(edge.Twin);

                result.Add(edge);
            }
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

    readonly record struct RemovalPlan(Node Node, ConstrainedRemoval? Constraint);

    readonly record struct Triangulation(
        Cavity Cavity,
        IReadOnlyList<TriangleIndices> Triangles);

    readonly record struct ConstrainedSpoke(Node Other, Edge[] HalfEdges);

    public readonly record struct ConstraintCounts(int Features, int Boundaries);

    public readonly record struct ConstrainedRemoval(
        Node First,
        Node Second,
        ConstraintCounts Forward,
        ConstraintCounts Reverse,
        EdgeConstraintState[] Edges);

    public readonly record struct EdgeConstraintState(
        Edge Edge,
        int Features,
        int Boundaries);
}
