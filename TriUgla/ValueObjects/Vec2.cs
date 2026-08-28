namespace TriUgla;

public readonly struct Vec2(double X, double Y)
{
    public override string ToString() => $"{X:F2} {Y:F2}";
}
