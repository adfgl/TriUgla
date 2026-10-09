namespace TriUgla.Tests;

public class QuadMeshTests
{
    [Fact]
    public void FromPairsTwoTrianglesIntoQuad()
    {
        (Mesh mesh, Node[] nodes, _) = CreateSquare();

        QuadMesh result = QuadMesh.From(mesh);

        QuadMeshFace quad = Assert.Single(result.Quads);
        Assert.Empty(result.Triangles);
        Assert.Equal(4, result.Nodes.Count);
        Face reconstructed = Assert.Single(result.Mesh.Faces());
        Assert.Equal(4, reconstructed.Edges.Count());
        Assert.DoesNotContain(reconstructed.Edges, edge => nodes.Contains(edge.NodeStart));
        Assert.All(result.Nodes, node => Assert.Equal(NodeKind.Normal, node.Kind));
        Vec2[] positions =
        [
            result.Nodes[quad.Indices.A].Position,
            result.Nodes[quad.Indices.B].Position,
            result.Nodes[quad.Indices.C].Position,
            result.Nodes[quad.Indices.D].Position
        ];
        Assert.Equal(nodes.Select(node => node.Position).ToHashSet(), positions.ToHashSet());
    }

    [Fact]
    public void FromRetainsTrianglesSeparatedByConstraint()
    {
        (Mesh mesh, _, Edge shared) = CreateSquare();
        shared.Constrain(EdgeConstraintKind.Feature);

        QuadMesh result = QuadMesh.From(mesh);

        Assert.Empty(result.Quads);
        Assert.Equal(2, result.Triangles.Count);
        QuadMeshConstraint constraint = Assert.Single(result.Constraints);
        Assert.Equal(1, constraint.FeatureConstraints);
        Assert.Equal(0, constraint.BoundaryConstraints);
    }

    [Fact]
    public void FromDoesNotPairFacesWithDifferentKinds()
    {
        (Mesh mesh, _, Edge shared) = CreateSquare();
        SetKind(shared.Face, FaceKind.Island);
        SetKind(shared.Twin!.Face, FaceKind.Lake);

        QuadMesh result = QuadMesh.From(mesh);

        Assert.Empty(result.Quads);
        Assert.Equal(2, result.Triangles.Count);
        Assert.Contains(result.Triangles, triangle => triangle.Kind == FaceKind.Island);
        Assert.Contains(result.Triangles, triangle => triangle.Kind == FaceKind.Lake);
    }

    [Fact]
    public void FromCopiesNodeValues()
    {
        (Mesh mesh, Node[] nodes, _) = CreateSquare();
        QuadMesh result = QuadMesh.From(mesh);
        Vec2 original = result.Nodes[0].Position;

        foreach (Node node in nodes) node.Position = new Vec2(99, 99);

        Assert.Equal(original, result.Nodes[0].Position);
    }

    [Fact]
    public void FromMayFlipPrivateTopologyWithoutChangingSource()
    {
        double[] angles =
        [
            1.9638657910074258,
            3.2718490346886986,
            4.046107846767979,
            5.076762772493932,
            5.961349503105239
        ];
        Mesh mesh = CreateFan(angles.Select(angle =>
            new Vec2(Math.Cos(angle) * 3d, Math.Sin(angle))).ToArray());
        Edge[] sourceEdges = CollectFaces(mesh.Root).SelectMany(face => face.Edges).ToArray();
        (Edge Edge, Edge? Twin, Edge Next, Edge Prev, Face Face)[] topology = sourceEdges
            .Select(edge => (edge, edge.Twin, edge.Next, edge.Prev, edge.Face)).ToArray();

        QuadMesh result = QuadMesh.From(mesh, new QuadMeshSettings(MaxEdgeFlips: 16));

        Assert.True(result.EdgeFlips > 0);
        foreach (var original in topology)
        {
            Assert.Same(original.Twin, original.Edge.Twin);
            Assert.Same(original.Next, original.Edge.Next);
            Assert.Same(original.Prev, original.Edge.Prev);
            Assert.Same(original.Face, original.Edge.Face);
        }
    }

    static (Mesh Mesh, Node[] Nodes, Edge Shared) CreateSquare()
    {
        Node[] nodes =
        [
            new() { Position = new Vec2(0, 0) },
            new() { Position = new Vec2(1, 0) },
            new() { Position = new Vec2(1, 1) },
            new() { Position = new Vec2(0, 1) }
        ];
        var first = new Face();
        var second = new Face();
        var ac = new Edge();
        var ca = new Edge();
        Linker.LinkTwins(ac, ca);
        Linker.LinkTriangle(first, new Edge(), new Edge(), ca, nodes[0], nodes[1], nodes[2]);
        Linker.LinkTriangle(second, ac, new Edge(), new Edge(), nodes[0], nodes[2], nodes[3]);
        return (new Mesh(first), nodes, ac);
    }

    static Mesh CreateFan(params Vec2[] positions)
    {
        Node[] nodes = positions.Select(position => new Node { Position = position }).ToArray();
        var unmatched = new Dictionary<(Node Start, Node End), Edge>();
        Face? root = null;
        for (int index = 1; index < nodes.Length - 1; index++)
        {
            var face = new Face();
            var edges = new[] { new Edge(), new Edge(), new Edge() };
            Linker.LinkTriangle(face, edges[0], edges[1], edges[2],
                nodes[0], nodes[index], nodes[index + 1]);
            root ??= face;
            foreach (Edge edge in edges)
            {
                if (unmatched.Remove((edge.NodeEnd, edge.NodeStart), out Edge? twin))
                    Linker.LinkTwins(edge, twin);
                else
                    unmatched.Add((edge.NodeStart, edge.NodeEnd), edge);
            }
        }
        return new Mesh(root!);
    }

    static IEnumerable<Face> CollectFaces(Face root)
    {
        var visited = new HashSet<Face> { root };
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

    static void SetKind(Face face, FaceKind kind)
        => typeof(Face).GetProperty(nameof(Face.Kind))!.SetValue(face, kind);
}
