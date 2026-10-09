namespace TriUgla;

public sealed class EdgeLegalizer
{
    readonly Func<Edge, bool?> _canFlip;
    readonly Func<Edge, EdgeFlipResult> _flip;
    readonly List<Face> _affected = new(32);
    readonly HashSet<Face> _affectedSet = new();
    readonly List<EdgeFlipRecord> _flips = new(32);

    public EdgeLegalizer(EdgeFlipper flipper)
    {
        ArgumentNullException.ThrowIfNull(flipper);
        _canFlip = edge => flipper.CanFlip(edge, out bool shouldFlip) ? shouldFlip : null;
        _flip = flipper.Flip;
    }

    public EdgeLegalizer(
        Func<Edge, bool?> canFlip,
        Func<Edge, EdgeFlipResult> flip)
    {
        _canFlip = canFlip ?? throw new ArgumentNullException(nameof(canFlip));
        _flip = flip ?? throw new ArgumentNullException(nameof(flip));
    }

    public EdgeLegalizationResult Legalize(Queue<Edge> illegalEdges)
    {
        _affected.Clear();
        _affectedSet.Clear();
        _flips.Clear();

        while (illegalEdges.TryDequeue(out Edge? edge))
        {
            AddAffected(edge.Face);

            if (_canFlip(edge) != true)
            {
                continue;
            }

            EdgeFlipResult result = _flip(edge);
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

        return new EdgeLegalizationResult(_affected, _flips);
    }

    void AddAffected(Face face)
    {
        // we should revisit duplicates..
        //if (_affectedSet.Add(face))
        {
            _affected.Add(face);
        }
    }
}
