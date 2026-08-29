namespace TriUgla.Tests;

public class SuperStructureTests
{
    [Fact]
    public void Make_Triangle_CreatesOneFace()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-1, -1),
            new Vec2(1, 1));

        Assert.Single(Faces(structure));
    }

    [Fact]
    public void Make_FiveSides_CreatesThreeFacesWithoutCenterNode()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-1, -1),
            new Vec2(1, 1),
            sideCount: 5);

        HashSet<Face> faces = Faces(structure);
        Node[] nodes = faces
            .SelectMany(face => face.Edges)
            .Select(edge => edge.NodeStart)
            .Distinct()
            .ToArray();

        Assert.Equal(3, faces.Count);
        Assert.Equal(5, structure.Nodes.Count);
        Assert.Equal(5, nodes.Length);
        Assert.DoesNotContain(nodes, node => node.Position == Vec2.Zero);
    }

    [Fact]
    public void Make_LinksEveryTriangleCycle()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-2, -1),
            new Vec2(2, 1),
            sideCount: 6);

        foreach (Face face in Faces(structure))
        {
            Edge[] edges = face.Edges.ToArray();

            Assert.Equal(3, edges.Length);
            Assert.All(edges, edge => Assert.Same(face, edge.Face));
            Assert.Same(edges[1], edges[0].Next);
            Assert.Same(edges[2], edges[1].Next);
            Assert.Same(edges[0], edges[2].Next);
            Assert.Same(edges[2], edges[0].Prev);
        }
    }

    [Fact]
    public void Make_LinksInternalEdgesAsTwins()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-1, -1),
            new Vec2(1, 1),
            sideCount: 5);

        Edge[] edges = Faces(structure).SelectMany(face => face.Edges).ToArray();
        Edge[] internalEdges = edges.Where(edge => edge.Twin is not null).ToArray();

        Assert.Equal(4, internalEdges.Length);
        Assert.All(internalEdges, edge =>
        {
            Assert.Same(edge, edge.Twin!.Twin);
            Assert.Same(edge.NodeStart, edge.Twin.NodeEnd);
            Assert.Same(edge.NodeEnd, edge.Twin.NodeStart);
        });
    }

    [Fact]
    public void SuperNode_ReturnsTrueOnlyForStructureNodes()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-1, -1),
            new Vec2(1, 1),
            sideCount: 4);
        Node superNode = structure.Root.Edge.NodeStart;

        Assert.True(structure.SuperNode(superNode));
        Assert.False(structure.SuperNode(new Node { Position = superNode.Position }));
    }

    [Fact]
    public void SuperFace_WithSuperNode_ReturnsTrue()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-1, -1),
            new Vec2(1, 1),
            sideCount: 4);

        Assert.True(structure.SuperFace(structure.Root));
    }

    [Fact]
    public void SuperFace_WithoutSuperNode_ReturnsFalse()
    {
        SuperStructure structure = SuperStructure.Make(
            new Vec2(-1, -1),
            new Vec2(1, 1),
            sideCount: 4);
        var face = new Face();

        Linker.LinkTriangle(
            face,
            new Edge(), new Edge(), new Edge(),
            new Node(), new Node(), new Node());

        Assert.False(structure.SuperFace(face));
    }

    [Fact]
    public void Make_FewerThanThreeSides_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => SuperStructure.Make(
            Vec2.Zero,
            Vec2.UnitX,
            sideCount: 2));

    static HashSet<Face> Faces(SuperStructure structure)
    {
        var visited = new HashSet<Face> { structure.Root };
        var pending = new Stack<Face>();
        pending.Push(structure.Root);

        while (pending.TryPop(out Face? face))
        {
            foreach (Face neighbour in face.Edges
                         .Select(edge => edge.Twin?.Face)
                         .OfType<Face>())
            {
                if (visited.Add(neighbour))
                {
                    pending.Push(neighbour);
                }
            }
        }

        return visited;
    }
}
