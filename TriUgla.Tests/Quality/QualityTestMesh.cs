namespace TriUgla.Tests;

static class QualityTestMesh
{
    public static Face Triangle(
        Vec2 a,
        Vec2 b,
        Vec2 c,
        NodeData aData = default,
        NodeData bData = default,
        NodeData cData = default)
    {
        Node[] nodes =
        [
            new Node { Position = a, Data = aData },
            new Node { Position = b, Data = bData },
            new Node { Position = c, Data = cData }
        ];
        Edge[] edges = [new(), new(), new()];
        Face face = new();
        Linker.LinkTriangle(face, edges[0], edges[1], edges[2], nodes[0], nodes[1], nodes[2]);
        return face;
    }
}
