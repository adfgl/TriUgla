namespace TriUgla.Tests;

public class EarClipperTests
{
    [Fact]
    public void TryTriangulate_ConcavePolygon_CreatesNMinusTwoTriangles()
    {
        Node[] polygon =
        [
            At(0, 0),
            At(2, 0),
            At(1, 1),
            At(2, 2),
            At(0, 2)
        ];

        bool success = EarClipper.TryTriangulate(polygon, out var triangles);

        Assert.True(success);
        Assert.Equal(3, triangles.Count);
        Assert.All(triangles, triangle => Assert.True(TriangleArea(polygon, triangle) > 0));
    }

    static Node At(double x, double y) => new() { Position = new Vec2(x, y) };

    static double TriangleArea(IReadOnlyList<Node> nodes, TriangleIndices triangle)
        => (nodes[triangle.B].Position - nodes[triangle.A].Position)
            .Cross(nodes[triangle.C].Position - nodes[triangle.A].Position) / 2;
}
