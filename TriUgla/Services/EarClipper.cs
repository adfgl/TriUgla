namespace TriUgla;

public static class EarClipper
{
    const double Epsilon = 1e-12;

    public static bool TryTriangulate(
        IReadOnlyList<Node> polygon,
        out IReadOnlyList<TriangleIndices> triangles)
    {
        if (polygon.Count < 3 || SignedArea(polygon) <= Epsilon)
        {
            triangles = [];
            return false;
        }

        var remaining = Enumerable.Range(0, polygon.Count).ToList();
        var result = new List<TriangleIndices>(polygon.Count - 2);

        while (remaining.Count > 3)
        {
            int ear = FindEar(polygon, remaining);
            if (ear < 0)
            {
                triangles = [];
                return false;
            }

            int previous = remaining[(ear - 1 + remaining.Count) % remaining.Count];
            int current = remaining[ear];
            int next = remaining[(ear + 1) % remaining.Count];
            result.Add(new TriangleIndices(previous, current, next));
            remaining.RemoveAt(ear);
        }

        result.Add(new TriangleIndices(remaining[0], remaining[1], remaining[2]));
        triangles = result;
        return true;
    }

    static int FindEar(IReadOnlyList<Node> polygon, IReadOnlyList<int> remaining)
    {
        for (int index = 0; index < remaining.Count; index++)
        {
            int previous = remaining[(index - 1 + remaining.Count) % remaining.Count];
            int current = remaining[index];
            int next = remaining[(index + 1) % remaining.Count];

            Vec2 a = polygon[previous].Position;
            Vec2 b = polygon[current].Position;
            Vec2 c = polygon[next].Position;

            if ((b - a).Cross(c - b) <= Epsilon)
            {
                continue;
            }

            bool containsVertex = remaining.Any(candidate =>
                candidate != previous &&
                candidate != current &&
                candidate != next &&
                IsInsideTriangle(polygon[candidate].Position, a, b, c));

            if (!containsVertex)
            {
                return index;
            }
        }

        return -1;
    }

    static bool IsInsideTriangle(Vec2 point, Vec2 a, Vec2 b, Vec2 c)
        => (b - a).Cross(point - a) >= -Epsilon &&
           (c - b).Cross(point - b) >= -Epsilon &&
           (a - c).Cross(point - c) >= -Epsilon;

    static double SignedArea(IReadOnlyList<Node> polygon)
    {
        double twiceArea = 0;
        for (int index = 0; index < polygon.Count; index++)
        {
            Vec2 current = polygon[index].Position;
            Vec2 next = polygon[(index + 1) % polygon.Count].Position;
            twiceArea += current.Cross(next);
        }

        return twiceArea / 2;
    }
}
