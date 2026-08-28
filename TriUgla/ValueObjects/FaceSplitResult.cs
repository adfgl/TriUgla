namespace TriUgla;

public readonly record struct FaceSplitResult(
    IReadOnlyList<Face> AffectedFaces,
    IReadOnlyList<Edge> EdgesToLegalize);
