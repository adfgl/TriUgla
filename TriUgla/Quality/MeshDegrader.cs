namespace TriUgla;

/// <summary>
/// Locally coarsens a mesh by trial-removing refinement Steiner nodes and
/// retaining only removals that do not increase target-area error.
/// </summary>
public sealed class MeshDegrader(Mesher mesher)
{
    const double AreaEpsilon = 1e-12;

    public int Degrade(IEnumerable<Node> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        int removed = 0;

        foreach (Node candidate in candidates.Distinct().ToArray())
        {
            if (candidate.Dead || candidate.Kind is not
                (NodeKind.SteinerRefinement or NodeKind.SteinerInsertion)) continue;

            Face[] beforeFaces = mesher.Mesh.Faces()
                .Where(face => !face.Dead && face.Contains(candidate))
                .ToArray();
            if (beforeFaces.Length == 0) continue;

            double before = Error(beforeFaces);
            if (!double.IsFinite(before)) continue;
            Vec2 position = candidate.Position;
            NodeData data = candidate.Data;
            NodeKind kind = candidate.Kind;
            RemoveNodeResult trial = mesher.Remove(candidate);
            if (!trial.Removed) continue;

            Face[] afterFaces = trial.Change.AffectedFaces.Where(face => !face.Dead).ToArray();
            double after = Error(afterFaces);
            if (double.IsFinite(after) && after <= before + 1e-12)
            {
                removed++;
                continue;
            }

            InsertNodeResult restored = mesher.Insert(position);
            if (restored.Node is null)
                throw new InvalidOperationException(
                    $"Could not restore rejected degradation candidate at {position}.");
            restored.Node.Data = data;
            restored.Node.Kind = kind;
        }

        return removed;
    }

    static double Error(IReadOnlyList<Face> faces)
    {
        double total = 0d;
        int measured = 0;
        foreach (Face face in faces)
        {
            double[] targets = face.Edges.Select(edge => edge.NodeStart.Data.Area)
                .Where(area => double.IsFinite(area) && area > AreaEpsilon)
                .ToArray();
            if (targets.Length == 0) continue;
            double target = targets.Average();
            total += Math.Abs(face.Area - target) / target;
            measured++;
        }
        return measured == 0 ? double.PositiveInfinity : total / measured;
    }
}
