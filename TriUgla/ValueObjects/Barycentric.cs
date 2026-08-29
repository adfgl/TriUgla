namespace TriUgla;

public readonly record struct Barycentric(double A, double B, double C)
{
    public static Barycentric FromSegment(Vec2 point, Vec2 start, Vec2 end)
    {
        Vec2 direction = end - start;
        if (direction.LengthSquared == 0d)
        {
            return new Barycentric(1d, 0d, 0d);
        }

        double amount = Math.Clamp(
            (point - start).Dot(direction) / direction.LengthSquared,
            0d,
            1d);
        return new Barycentric(1d - amount, amount, 0d);
    }

    public static Barycentric From(Vec2 point, Vec2 a, Vec2 b, Vec2 c)
    {
        double area = (b - a).Cross(c - a);

        if (area == 0)
        {
            return new Barycentric(1, 0, 0);
        }

        double weightA = (b - point).Cross(c - point) / area;
        double weightB = (c - point).Cross(a - point) / area;
        double weightC = 1 - weightA - weightB;

        return new Barycentric(weightA, weightB, weightC);
    }

    public double Interpolate(double a, double b, double c)
        => A * a + B * b + C * c;

    public Vec2 Interpolate(Vec2 a, Vec2 b, Vec2 c)
        => new(
            Interpolate(a.X, b.X, c.X),
            Interpolate(a.Y, b.Y, c.Y));

    public NodeData Interpolate(NodeData a, NodeData b, NodeData c)
        => new(
            Interpolate(a.Elevation, b.Elevation, c.Elevation),
            Interpolate(a.Area, b.Area, c.Area));

    public double Interpolate<T>(T a, T b, T c, Func<T, double> selector)
        => Interpolate(selector(a), selector(b), selector(c));
}
