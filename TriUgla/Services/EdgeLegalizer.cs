namespace TriUgla;

public sealed class EdgeLegalizer(IEdgeFlipper flipper) : IEdgeLegalizer
{
    readonly List<Face> _affected = new(32);
    readonly HashSet<Face> _affectedSet = new();
    readonly List<EdgeFlipRecord> _flips = new(32);

    public EdgeLegalizationResult Legalize(Queue<Edge> illegalEdges)
    {
        _affected.Clear();
        _affectedSet.Clear();
        _flips.Clear();

        while (illegalEdges.TryDequeue(out Edge? edge))
        {
            AddAffected(edge.Face);

            if (!flipper.CanFlip(edge, out bool shouldFlip) || !shouldFlip)
            {
                continue;
            }

            EdgeFlipResult result = flipper.Flip(edge);
            _flips.Add(new EdgeFlipRecord(result.FlippedEdge));
            TopologyChange change = result.Change;

            foreach (Face face in change.AffectedFaces)
            {
                AddAffected(face);
            }

            foreach (Edge candidate in change.EdgesToLegalize)
            {
                illegalEdges.Enqueue(candidate);
            }
        }

        return new EdgeLegalizationResult(
            _affected.ToArray(),
            _flips.ToArray());
    }

    void AddAffected(Face face)
    {
        if (_affectedSet.Add(face))
        {
            _affected.Add(face);
        }
    }
}
