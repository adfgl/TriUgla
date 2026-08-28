namespace TriUgla;

public readonly record struct Vec2(double X, double Y)
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
        => from + (to - from) * amount;

    public static Vec2 operator +(Vec2 left, Vec2 right)
        => new(left.X + right.X, left.Y + right.Y);

    public static Vec2 operator -(Vec2 left, Vec2 right)
        => new(left.X - right.X, left.Y - right.Y);

    public static Vec2 operator -(Vec2 value) => new(-value.X, -value.Y);

    public static Vec2 operator *(Vec2 value, double scalar)
        => new(value.X * scalar, value.Y * scalar);

    public static Vec2 operator *(double scalar, Vec2 value) => value * scalar;

    public static Vec2 operator /(Vec2 value, double scalar)
        => new(value.X / scalar, value.Y / scalar);

    public override string ToString() => $"({X}, {Y})";
}
