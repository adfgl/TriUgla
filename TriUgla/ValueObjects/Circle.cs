using System.Runtime.CompilerServices;

namespace TriUgla;

/// <summary>A two-dimensional circle stored as a center and squared radius.</summary>
/// <remarks>
/// Squared radius avoids square roots in containment tests. For background on
/// circumcenters and their role in Delaunay meshing, see Jonathan Shewchuk's
/// <see href="https://www.cs.cmu.edu/~quake/tripaper/triangle0.html">Triangle paper</see>.
/// For robust orientation and incircle classification near floating-point boundaries,
/// see <see href="https://www.cs.cmu.edu/~quake/robust.html">Robust Predicates</see>.
/// </remarks>
public readonly struct Circle(Vec2 center, double radiusSquared)
{
    public readonly Vec2 Center = center;
    public readonly double RadiusSquared = radiusSquared;

    /// <summary>Returns whether a point is strictly inside this circle.</summary>
    /// <remarks>
    /// The comparison uses squared distances. A small scale-aware margin excludes
    /// points numerically indistinguishable from the circumference; this method is
    /// intentionally not a replacement for an exact incircle predicate.
    /// </remarks>
    public bool Contains(in Vec2 point)
    {
        double dx = point.X - Center.X;
        double dy = point.Y - Center.Y;
        double distanceSquared = dx * dx + dy * dy;
        double epsilon = 1e-14 * (RadiusSquared + 1d);
        return distanceSquared < RadiusSquared - epsilon;
    }

    /// <summary>Creates the circle whose diameter is the segment between two points.</summary>
    public static Circle From2(Vec2 first, Vec2 second)
    {
        // A diameter circle is centered at the segment midpoint. Its radius is
        // half the segment length, hence radius² = segmentLength² / 4.
        double centerX = (first.X + second.X) * 0.5d;
        double centerY = (first.Y + second.Y) * 0.5d;
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        return new Circle(
            new Vec2(centerX, centerY),
            (dx * dx + dy * dy) * 0.25d);
    }

    /// <summary>Creates the circumcircle through three non-collinear points.</summary>
    /// <remarks>
    /// The center is equidistant from all three points. Expanding
    /// |center-first|² = |center-third|² and
    /// |center-second|² = |center-third|² cancels the quadratic center terms and
    /// leaves a 2×2 linear system. Cramer's rule solves that system below.
    /// Collinear inputs have a zero determinant and produce a non-finite circle.
    /// </remarks>
    public static Circle From3(Vec2 first, Vec2 second, Vec2 third)
    {
        double dx13 = first.X - third.X;
        double dy13 = first.Y - third.Y;
        double dx23 = second.X - third.X;
        double dy23 = second.Y - third.Y;
        double scale = Math.Max(
            Math.Max(Math.Abs(dx13), Math.Abs(dy13)),
            Math.Max(Math.Abs(dx23), Math.Abs(dy23)));
        if (scale == 0d || !double.IsFinite(scale))
            return new Circle(Vec2.Make(double.NaN), double.NaN);

        // Translation avoids cancellation from absolute coordinates; scaling
        // conditions the determinant and prevents squaring from overflowing or
        // underflowing. The final offsets are restored to the input scale.
        double ax = dx13 / scale;
        double ay = dy13 / scale;
        double bx = dx23 / scale;
        double by = dy23 / scale;
        double determinant = 2d * Determinant(ax, ay, bx, by);
        if (determinant == 0d)
            return new Circle(Vec2.Make(double.NaN), double.NaN);

        double aLength2 = ax * ax + ay * ay;
        double bLength2 = bx * bx + by * by;
        double centerOffsetX = Determinant(aLength2, ay, bLength2, by) / determinant * scale;
        double centerOffsetY = Determinant(ax, aLength2, bx, bLength2) / determinant * scale;
        double centerX = third.X + centerOffsetX;
        double centerY = third.Y + centerOffsetY;
        double radiusX = centerX - first.X;
        double radiusY = centerY - first.Y;
        return new Circle(
            new Vec2(centerX, centerY),
            radiusX * radiusX + radiusY * radiusY);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double Determinant(
        double m11,
        double m12,
        double m21,
        double m22)
        => m11 * m22 - m12 * m21;
}
