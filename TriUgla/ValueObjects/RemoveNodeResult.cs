namespace TriUgla;

public readonly record struct RemoveNodeResult(
    bool Removed,
    Node Node,
    TopologyChange Change,
    IReadOnlyList<Face> DeadFaces,
    IReadOnlyList<Edge> DeadEdges)
{
    public static RemoveNodeResult Failed(Node node)
        => new(false, node, TopologyChange.Empty, [], []);
}
