using System.Runtime.CompilerServices;

namespace TriUgla;

public abstract class FaceRankAspectBase : FaceRankAspect
{
    protected const double Epsilon = 1e-24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static double SafeDivide(double numerator, double denominator)
        => numerator / Math.Max(denominator, Epsilon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);
}
