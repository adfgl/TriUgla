namespace TriUgla;

public readonly record struct EdgeFlipResult(
    Edge FlippedEdge,
    IReadOnlyList<Face> AffectedFaces,
    IReadOnlyList<Edge> EdgesToLegalize);
