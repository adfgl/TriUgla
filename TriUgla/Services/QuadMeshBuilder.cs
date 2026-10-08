namespace TriUgla;

/// <summary>Builds a detached quad-dominant representation without modifying its source mesh.</summary>
public sealed class QuadMeshBuilder
{
    public QuadMesh Build(Mesh source) => Build(source, new QuadMeshSettings());

    public QuadMesh Build(Mesh source, in QuadMeshSettings settings)
    {
        ArgumentNullException.ThrowIfNull(source);
        Validate(settings);
        ReconstructionMesh mesh = ReconstructionMesh.From(source);
        var evaluator = new QuadCandidateEvaluator(mesh, settings.MinimumQuadQuality);
        int flips = settings.AllowEdgeFlips
            ? new QuadReconstructor(mesh, evaluator).Improve(settings.MaxEdgeFlips)
            : 0;
        PairingResult pairing = new TrianglePairer(mesh, evaluator).Pair();

        QuadMeshFace[] quads = new QuadMeshFace[pairing.Candidates.Length];
        var paired = new bool[mesh.Triangles.Length];
        for (int index = 0; index < pairing.Candidates.Length; index++)
        {
            QuadCandidate candidate = pairing.Candidates[index];
            quads[index] = new QuadMeshFace(candidate.Quad, mesh.Triangles[candidate.First].Kind);
            paired[candidate.First] = paired[candidate.Second] = true;
        }

        var triangles = new List<QuadMeshTriangle>(mesh.Triangles.Length - pairing.Candidates.Length * 2);
        for (int index = 0; index < mesh.Triangles.Length; index++)
        {
            if (paired[index]) continue;
            ReconstructionTriangle triangle = mesh.Triangles[index];
            triangles.Add(new QuadMeshTriangle(
                new TriangleIndices(triangle.A, triangle.B, triangle.C), triangle.Kind));
        }
        return new QuadMesh(mesh.Nodes, quads, triangles.ToArray(), mesh.Constraints, flips);
    }

    static void Validate(in QuadMeshSettings settings)
    {
        if (settings.MaxEdgeFlips < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "MaxEdgeFlips cannot be negative.");
        if (!double.IsFinite(settings.MinimumQuadQuality) ||
            settings.MinimumQuadQuality < 0d || settings.MinimumQuadQuality > 1d)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "MinimumQuadQuality must be between zero and one.");
    }
}
