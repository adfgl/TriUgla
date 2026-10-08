namespace TriUgla;

internal sealed class QuadReconstructor(
    ReconstructionMesh mesh,
    QuadCandidateEvaluator evaluator)
{
    readonly TrianglePairer _pairer = new(mesh, evaluator);

    public int Improve(int maximumFlips)
    {
        int accepted = 0;
        while (accepted < maximumFlips && TryImprove()) accepted++;
        return accepted;
    }

    bool TryImprove()
    {
        // Snapshot keys because accepted/reverted flips update the adjacency dictionary.
        EdgeKey[] edges = mesh.Edges
            .Where(item => item.Value.Internal && !mesh.Locked(item.Key))
            .Select(item => item.Key)
            .ToArray();
        foreach (EdgeKey edge in edges)
        {
            if (!mesh.TryGetAdjacency(edge, out EdgeAdjacency adjacency) ||
                !evaluator.TryFlip(edge, adjacency,
                    out ReconstructionTriangle replacementFirst,
                    out ReconstructionTriangle replacementSecond)) continue;

            HashSet<int> neighbourhood = mesh.Neighbourhood(adjacency.First, adjacency.Second);
            PairingResult before = _pairer.Pair(neighbourhood);
            ReconstructionTriangle originalFirst = mesh.Triangles[adjacency.First];
            ReconstructionTriangle originalSecond = mesh.Triangles[adjacency.Second];
            mesh.Replace(adjacency.First, replacementFirst, adjacency.Second, replacementSecond);
            PairingResult after = _pairer.Pair(neighbourhood);
            if (Better(after, before)) return true;
            mesh.Replace(adjacency.First, originalFirst, adjacency.Second, originalSecond);
        }
        return false;
    }

    static bool Better(PairingResult candidate, PairingResult current)
        => candidate.Candidates.Length > current.Candidates.Length ||
            candidate.Candidates.Length == current.Candidates.Length &&
            candidate.Quality > current.Quality + 1e-12;
}
