namespace TriUgla;

public readonly record struct Barycentric(double A, double B, double C)
{
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

    public double Interpolate<T>(T a, T b, T c, Func<T, double> selector)
        => Interpolate(selector(a), selector(b), selector(c));
}
