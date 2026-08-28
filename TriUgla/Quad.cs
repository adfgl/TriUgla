namespace TriUgla;

public readonly record struct Quad(Node A, Node B, Node C, Node D)
{
    public static Quad From(Edge edge)
    {
        Edge? twin = edge.Twin ?? throw new InvalidOperationException(
                "Cannot create a quad from an edge without a twin.");
        return new Quad(
            edge.NodeStart,
            twin.Prev.NodeStart,
            edge.NodeEnd,
            edge.Prev.NodeStart);
    }
}
