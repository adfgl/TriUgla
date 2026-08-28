namespace TriUgla;

public readonly record struct EdgeInsertResult(
    IReadOnlyList<Edge> ConstrainedEdges,
    IReadOnlyList<Node> InsertedNodes,
    TopologyChange Change);
