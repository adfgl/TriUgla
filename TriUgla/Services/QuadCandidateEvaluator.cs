namespace TriUgla;

internal readonly record struct QuadCandidate(
    EdgeKey Edge, int First, int Second, QuadIndices Quad, double Quality);

internal sealed class QuadCandidateEvaluator(
    ReconstructionMesh mesh,
    double minimumQuality)
{
    readonly GeometryPredicates _geometry = new();

    public bool TryEvaluate(EdgeKey edge, EdgeAdjacency adjacency, out QuadCandidate candidate)
    {
        candidate = default;
        if (!adjacency.Internal || mesh.Locked(edge)) return false;
        ReconstructionTriangle first = mesh.Triangles[adjacency.First];
        ReconstructionTriangle second = mesh.Triangles[adjacency.Second];
        if (first.Kind != second.Kind ||
            !TryQuad(first, second, edge, out QuadIndices quad) ||
            !IsConvex(quad)) return false;
        double quality = Quality(quad);
        if (quality < minimumQuality) return false;
        candidate = new QuadCandidate(
            edge, adjacency.First, adjacency.Second, quad, quality);
        return true;
    }

    public bool TryFlip(EdgeKey edge, EdgeAdjacency adjacency,
        out ReconstructionTriangle first, out ReconstructionTriangle second)
    {
        first = second = default;
        if (!adjacency.Internal || mesh.Locked(edge)) return false;
        ReconstructionTriangle currentFirst = mesh.Triangles[adjacency.First];
        ReconstructionTriangle currentSecond = mesh.Triangles[adjacency.Second];
        if (currentFirst.Kind != currentSecond.Kind ||
            !TryQuad(currentFirst, currentSecond, edge, out QuadIndices quad) ||
            !IsConvex(quad)) return false;

        var replacement = new EdgeKey(quad.B, quad.D);
        if (mesh.Locked(replacement) || mesh.TryGetAdjacency(replacement, out _)) return false;
        first = new ReconstructionTriangle(quad.A, quad.B, quad.D, currentFirst.Kind);
        second = new ReconstructionTriangle(quad.B, quad.C, quad.D, currentFirst.Kind);
        return true;
    }

    static bool TryQuad(ReconstructionTriangle first, ReconstructionTriangle second,
        EdgeKey shared, out QuadIndices quad)
    {
        if (!TryDirected(first, shared, out int start, out int end, out int firstOpposite) ||
            !TryDirected(second, shared, out int reverseStart, out int reverseEnd,
                out int secondOpposite) ||
            start != reverseEnd || end != reverseStart || firstOpposite == secondOpposite)
        {
            quad = default;
            return false;
        }
        quad = new QuadIndices(start, secondOpposite, end, firstOpposite);
        return true;
    }

    static bool TryDirected(ReconstructionTriangle triangle, EdgeKey target,
        out int start, out int end, out int opposite)
    {
        if (new EdgeKey(triangle.A, triangle.B) == target)
        { start = triangle.A; end = triangle.B; opposite = triangle.C; return true; }
        if (new EdgeKey(triangle.B, triangle.C) == target)
        { start = triangle.B; end = triangle.C; opposite = triangle.A; return true; }
        if (new EdgeKey(triangle.C, triangle.A) == target)
        { start = triangle.C; end = triangle.A; opposite = triangle.B; return true; }
        start = end = opposite = -1;
        return false;
    }

    bool IsConvex(QuadIndices quad)
    {
        Vec2 a = mesh.Nodes[quad.A].Position;
        Vec2 b = mesh.Nodes[quad.B].Position;
        Vec2 c = mesh.Nodes[quad.C].Position;
        Vec2 d = mesh.Nodes[quad.D].Position;
        int orientation = _geometry.OrientSign(d, a, b);
        return orientation != 0 &&
            _geometry.OrientSign(a, b, c) == orientation &&
            _geometry.OrientSign(b, c, d) == orientation &&
            _geometry.OrientSign(c, d, a) == orientation;
    }

    double Quality(QuadIndices quad)
    {
        Vec2 a = mesh.Nodes[quad.A].Position;
        Vec2 b = mesh.Nodes[quad.B].Position;
        Vec2 c = mesh.Nodes[quad.C].Position;
        Vec2 d = mesh.Nodes[quad.D].Position;
        double ab = (b - a).Length;
        double bc = (c - b).Length;
        double cd = (d - c).Length;
        double da = (a - d).Length;
        double longest = Math.Max(Math.Max(ab, bc), Math.Max(cd, da));
        if (longest == 0d) return 0d;
        double shortest = Math.Min(Math.Min(ab, bc), Math.Min(cd, da));
        double angles = Math.Min(
            Math.Min(RightAngleQuality(d - a, b - a), RightAngleQuality(a - b, c - b)),
            Math.Min(RightAngleQuality(b - c, d - c), RightAngleQuality(c - d, a - d)));
        return Math.Clamp(angles * shortest / longest, 0d, 1d);
    }

    static double RightAngleQuality(Vec2 first, Vec2 second)
    {
        double denominator = first.Length * second.Length;
        return denominator == 0d ? 0d : 1d - Math.Abs(first.Dot(second) / denominator);
    }
}
