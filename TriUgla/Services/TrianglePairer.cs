namespace TriUgla;

internal readonly record struct PairingResult(QuadCandidate[] Candidates, double Quality);

internal sealed class TrianglePairer(
    ReconstructionMesh mesh,
    QuadCandidateEvaluator evaluator)
{
    public PairingResult Pair(IReadOnlySet<int>? included = null)
    {
        var candidates = new List<QuadCandidate>();
        if (included is null)
        {
            foreach ((EdgeKey edge, EdgeAdjacency adjacency) in mesh.Edges)
                AddCandidate(edge, adjacency);
        }
        else
        {
            var localEdges = new HashSet<EdgeKey>();
            foreach (int triangle in included) mesh.Triangles[triangle].AddEdges(localEdges);
            foreach (EdgeKey edge in localEdges)
            {
                if (!mesh.TryGetAdjacency(edge, out EdgeAdjacency adjacency) ||
                    !included.Contains(adjacency.First) ||
                    !included.Contains(adjacency.Second)) continue;
                AddCandidate(edge, adjacency);
            }
        }
        candidates.Sort(static (first, second) =>
        {
            int quality = second.Quality.CompareTo(first.Quality);
            if (quality != 0) return quality;
            int triangle = first.First.CompareTo(second.First);
            return triangle != 0 ? triangle : first.Second.CompareTo(second.Second);
        });

        var selected = new List<QuadCandidate>(candidates.Count);
        double qualitySum = 0d;
        if (included is null)
        {
            var used = new bool[mesh.Triangles.Length];
            foreach (QuadCandidate candidate in candidates)
            {
                if (used[candidate.First] || used[candidate.Second]) continue;
                used[candidate.First] = used[candidate.Second] = true;
                Select(candidate);
            }
        }
        else
        {
            var used = new HashSet<int>();
            foreach (QuadCandidate candidate in candidates)
            {
                if (!used.Add(candidate.First)) continue;
                if (!used.Add(candidate.Second))
                {
                    used.Remove(candidate.First);
                    continue;
                }
                Select(candidate);
            }
        }
        return new PairingResult(selected.ToArray(), qualitySum);

        void AddCandidate(EdgeKey edge, EdgeAdjacency adjacency)
        {
            if (evaluator.TryEvaluate(edge, adjacency, out QuadCandidate candidate))
                candidates.Add(candidate);
        }

        void Select(QuadCandidate candidate)
        {
            selected.Add(candidate);
            qualitySum += candidate.Quality;
        }
    }
}
