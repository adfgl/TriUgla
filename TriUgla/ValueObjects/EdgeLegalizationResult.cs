namespace TriUgla;

public readonly record struct EdgeLegalizationResult(
    IReadOnlyList<Face> AffectedFaces,
    IReadOnlyList<EdgeFlipRecord> Flips);
