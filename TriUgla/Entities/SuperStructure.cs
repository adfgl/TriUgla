namespace TriUgla;

public sealed class SuperStructure
{
    const double DefaultScale = 2;

    public Face Root { get; }
    public HashSet<Node> Nodes { get; }

    SuperStructure(Face root, HashSet<Node> nodes)
    {
        Root = root;
        Nodes = nodes;
    }

    public bool SuperNode(Node node) => Nodes.Contains(node);

    public bool SuperFace(Face face)
        => face.Edges.Any(edge => SuperNode(edge.NodeStart));

    public static SuperStructure Make(Vec2 min, Vec2 max, int sideCount = 3)
    {
        if (sideCount < 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sideCount),
                sideCount,
                "A super structure requires at least three sides.");
        }

        (min, max) = OrderBounds(min, max);

        Node[] nodes = MakeCircularNodes(min, max, sideCount);
        Face root = Triangulate(nodes);

        return new SuperStructure(root, nodes.ToHashSet());
    }

    static (Vec2 Min, Vec2 Max) OrderBounds(Vec2 first, Vec2 second)
        => (Vec2.Min(first, second), Vec2.Max(first, second));

    static Node[] MakeCircularNodes(Vec2 min, Vec2 max, int count)
    {
        Vec2 center = (min + max) / 2;
        double halfDiagonal = (max - min).Length / 2;
        double radius = halfDiagonal * DefaultScale / Math.Cos(Math.PI / count);

        var nodes = new Node[count];

        for (int index = 0; index < count; index++)
        {
            double angle = -Math.PI / 2 + index * 2 * Math.PI / count;
            nodes[index] = new Node
            {
                Position = center + radius * new Vec2(Math.Cos(angle), Math.Sin(angle))
            };
        }

        return nodes;
    }

    static Face Triangulate(IReadOnlyList<Node> nodes)
    {
        var unmatchedEdges = new Dictionary<(Node Start, Node End), Edge>();
        Face? root = null;

        for (int index = 1; index < nodes.Count - 1; index++)
        {
            Face face = MakeTriangle(nodes[0], nodes[index], nodes[index + 1]);
            root ??= face;
            LinkInternalEdges(face, unmatchedEdges);
        }

        return root!;
    }

    static Face MakeTriangle(Node a, Node b, Node c)
    {
        var face = new Face();
        Linker.LinkTriangle(
            face,
            new Edge(), new Edge(), new Edge(),
            a, b, c);
        return face;
    }

    static void LinkInternalEdges(
        Face face,
        Dictionary<(Node Start, Node End), Edge> unmatchedEdges)
    {
        foreach (Edge edge in face.Edges)
        {
            var reverse = (edge.NodeEnd, edge.NodeStart);

            if (unmatchedEdges.Remove(reverse, out Edge? twin))
            {
                Linker.LinkTwins(edge, twin);
            }
            else
            {
                unmatchedEdges.Add((edge.NodeStart, edge.NodeEnd), edge);
            }
        }
    }
}
