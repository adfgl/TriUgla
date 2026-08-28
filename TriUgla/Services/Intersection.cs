namespace TriUgla;

public static class Intersection
{
    public static bool Intersect(
        Vec2 p1,
        Vec2 p2,
        Vec2 q1,
        Vec2 q2,
        out Vec2 intersection,
        double parallelEpsilon = 1e-12,
        double parameterEpsilon = 1e-12)
    {
        ValidateEpsilon(parallelEpsilon, nameof(parallelEpsilon));
        ValidateEpsilon(parameterEpsilon, nameof(parameterEpsilon));

        intersection = Vec2.Make(double.NaN);

        Vec2 pDirection = p2 - p1;
        Vec2 qDirection = q2 - q1;

        if (!TryGetParameters(
                q1 - p1,
                pDirection,
                qDirection,
                parallelEpsilon,
                out double pParameter,
                out double qParameter))
        {
            return false;
        }

        if (!IsOnSegment(pParameter, parameterEpsilon) ||
            !IsOnSegment(qParameter, parameterEpsilon))
        {
            return false;
        }

        intersection = PointOnSegment(p1, pDirection, pParameter);
        return true;
    }

    static bool IsZero(double value, double epsilon)
        => Math.Abs(value) <= epsilon;

    static bool TryGetParameters(
        Vec2 offset,
        Vec2 pDirection,
        Vec2 qDirection,
        double parallelEpsilon,
        out double pParameter,
        out double qParameter)
    {
        double denominator = pDirection.Cross(qDirection);

        if (IsZero(denominator, parallelEpsilon))
        {
            pParameter = qParameter = 0;
            return false;
        }

        pParameter = offset.Cross(qDirection) / denominator;
        qParameter = offset.Cross(pDirection) / denominator;
        return true;
    }

    static bool IsOnSegment(double parameter, double epsilon)
        => parameter >= -epsilon && parameter <= 1 + epsilon;

    static Vec2 PointOnSegment(Vec2 start, Vec2 direction, double parameter)
        => start + direction * Math.Clamp(parameter, 0, 1);

    static void ValidateEpsilon(double epsilon, string parameterName)
    {
        if (double.IsNaN(epsilon) || epsilon < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                epsilon,
                "Epsilon must be a non-negative number.");
        }
    }
}
