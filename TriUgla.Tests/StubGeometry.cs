namespace TriUgla.Tests;

sealed class StubGeometry : IGeometry
{
    public bool Convex { get; set; } = true;
    public bool InsideCircumcircle { get; set; }

    public EOrientaiton Orient(Node a, Node b, Vec2 point) => default;

    public EOrientaiton Orient(Edge edge, Vec2 point) => default;

    public bool InDiameterCircle(Node a, Node b, Vec2 point) => false;

    public bool InCircumcircle(Node a, Node b, Node c, Vec2 point)
        => InsideCircumcircle;

    public bool IsConvexQuad(Quad quad) => Convex;
}
