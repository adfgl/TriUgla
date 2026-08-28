namespace TriUgla;

public readonly record struct TopologyChange(
    IReadOnlyList<Face> AffectedFaces,
    IReadOnlyList<Edge> EdgesToLegalize)
{
    public static TopologyChange Empty => new([], []);
}
