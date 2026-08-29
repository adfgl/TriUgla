namespace TriUgla;

public readonly record struct Vec2(double X, double Y, double Z = 0d, double W = 1d)
{
    public static readonly Vec2 Zero = new(0, 0);
    public static readonly Vec2 UnitX = new(1, 0);
    public static readonly Vec2 UnitY = new(0, 1);

    public double Length => Math.Sqrt(LengthSquared);

    public double LengthSquared => X * X + Y * Y;

    public static Vec2 Make(double value) => new(value, value);

    public double Max() => Math.Max(X, Y);

    public static Vec2 Min(Vec2 first, Vec2 second) => new(
        Math.Min(first.X, second.X),
        Math.Min(first.Y, second.Y));

    public static Vec2 Max(Vec2 first, Vec2 second) => new(
        Math.Max(first.X, second.X),
        Math.Max(first.Y, second.Y));

    public Vec2 Normalize()
    {
        double length = Length;
        return length > 0 ? this / length : Zero;
    }

    public double Dot(Vec2 other) => X * other.X + Y * other.Y;

    public double Cross(Vec2 other) => X * other.Y - Y * other.X;

    public double Distance(Vec2 other) => (other - this).Length;

    public double DistanceSquared(Vec2 other) => (other - this).LengthSquared;

    public static Vec2 Lerp(Vec2 from, Vec2 to, double amount)
        => new(
            from.X + (to.X - from.X) * amount,
            from.Y + (to.Y - from.Y) * amount,
            from.Z + (to.Z - from.Z) * amount,
            from.W + (to.W - from.W) * amount);

    public static Vec2 operator +(Vec2 left, Vec2 right)
        => new(left.X + right.X, left.Y + right.Y, left.Z, left.W);

    public static Vec2 operator -(Vec2 left, Vec2 right)
        => new(left.X - right.X, left.Y - right.Y, left.Z, left.W);

    public static Vec2 operator -(Vec2 value) => new(-value.X, -value.Y, value.Z, value.W);

    public static Vec2 operator *(Vec2 value, double scalar)
        => new(value.X * scalar, value.Y * scalar, value.Z, value.W);

    public static Vec2 operator *(double scalar, Vec2 value) => value * scalar;

    public static Vec2 operator /(Vec2 value, double scalar)
        => new(value.X / scalar, value.Y / scalar, value.Z, value.W);

    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}
