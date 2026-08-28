namespace TriUgla;

public readonly record struct MeshSnapshot(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Edge> Edges,
    IReadOnlyList<Face> Faces);
