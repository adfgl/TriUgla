namespace TriUgla;

/// <summary>
/// Adaptive robust geometric predicates. Fast floating-point filters handle
/// well-conditioned inputs; expansion arithmetic resolves uncertain signs exactly.
/// </summary>
public sealed class GeometryPredicates : IGeometry
{
    const double UnitRoundoff = 1.1102230246251565e-16;

    public bool AllowExactMath { get; set; } = true;
    public int ExactOrientationComputations { get; private set; }
    public int ExactInCircleComputations { get; private set; }

    /// <summary>
    /// Classifies two closed segments: -1 disjoint, 0 endpoint/tangent contact,
    /// 1 proper crossing, and 2 collinear overlap.
    /// </summary>
    public int Intersects(Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2)
    {
        int p1p2q1 = OrientSign(p1, p2, q1);
        int p1p2q2 = OrientSign(p1, p2, q2);
        int q1q2p1 = OrientSign(q1, q2, p1);
        int q1q2p2 = OrientSign(q1, q2, p2);

        if (p1p2q1 == 0 && p1p2q2 == 0 && q1q2p1 == 0 && q1q2p2 == 0)
        {
            return CollinearOverlap(p1, p2, q1, q2) ? 2 : -1;
        }

        if (p1p2q1 != 0 && p1p2q2 != 0 && q1q2p1 != 0 && q1q2p2 != 0)
        {
            return p1p2q1 != p1p2q2 && q1q2p1 != q1q2p2 ? 1 : -1;
        }

        if (p1p2q1 == 0 && OnSegment(p1, p2, q1)) return 0;
        if (p1p2q2 == 0 && OnSegment(p1, p2, q2)) return 0;
        if (q1q2p1 == 0 && OnSegment(q1, q2, p1)) return 0;
        if (q1q2p2 == 0 && OnSegment(q1, q2, p2)) return 0;
        return -1;
    }

    public static bool CollinearOverlap(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        => Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X)) <=
               Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) &&
           Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)) <=
               Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));

    public static bool OnSegment(Vec2 a, Vec2 b, Vec2 point)
        => point.X >= Math.Min(a.X, b.X) && point.X <= Math.Max(a.X, b.X) &&
           point.Y >= Math.Min(a.Y, b.Y) && point.Y <= Math.Max(a.Y, b.Y);

    public EOrientaiton Orient(Node a, Node b, Vec2 point)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return OrientSign(a.Position, b.Position, point) switch
        {
            > 0 => EOrientaiton.Counterclockwise,
            < 0 => EOrientaiton.Clockwise,
            _ => EOrientaiton.Collinear
        };
    }

    public EOrientaiton Orient(Edge edge, Vec2 point)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return Orient(edge.NodeStart, edge.NodeEnd, point);
    }

    public bool InDiameterCircle(Node a, Node b, Vec2 point)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return InDiameterCircleSign(a.Position, b.Position, point) > 0;
    }

    public bool InCircumcircle(Node a, Node b, Node c, Vec2 point)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(c);
        return InCircleSign(a.Position, b.Position, c.Position, point) > 0;
    }

    public bool IsConvexQuad(Quad quad)
    {
        int orientation = OrientSign(quad.D.Position, quad.A.Position, quad.B.Position);
        return orientation != 0 &&
               OrientSign(quad.A.Position, quad.B.Position, quad.C.Position) == orientation &&
               OrientSign(quad.B.Position, quad.C.Position, quad.D.Position) == orientation &&
               OrientSign(quad.C.Position, quad.D.Position, quad.A.Position) == orientation;
    }

    public int OrientSign(Vec2 a, Vec2 b, Vec2 c)
    {
        RequireFinite(a, nameof(a));
        RequireFinite(b, nameof(b));
        RequireFinite(c, nameof(c));

        double bax = b.X - a.X;
        double bay = b.Y - a.Y;
        double cax = c.X - a.X;
        double cay = c.Y - a.Y;
        double positive = bax * cay;
        double negative = bay * cax;
        double determinant = positive - negative;
        double errorBound = 16d * UnitRoundoff * (Math.Abs(positive) + Math.Abs(negative));
        if (determinant > errorBound) return 1;
        if (determinant < -errorBound) return -1;
        if (!AllowExactMath) return Math.Sign(determinant);

        ExactOrientationComputations++;
        List<double> exact = Cross(
            Difference(b.X, a.X),
            Difference(b.Y, a.Y),
            Difference(c.X, a.X),
            Difference(c.Y, a.Y));
        return Expansion.Sign(exact);
    }

    public int InDiameterCircleSign(Vec2 a, Vec2 b, Vec2 point)
    {
        RequireFinite(a, nameof(a));
        RequireFinite(b, nameof(b));
        RequireFinite(point, nameof(point));

        Vec2 fromA = point - a;
        Vec2 fromB = point - b;
        double xProduct = fromA.X * fromB.X;
        double yProduct = fromA.Y * fromB.Y;
        double dot = xProduct + yProduct;
        double errorBound = 16d * UnitRoundoff * (Math.Abs(xProduct) + Math.Abs(yProduct));
        if (dot > errorBound) return -1;
        if (dot < -errorBound) return 1;
        if (!AllowExactMath) return dot < 0d ? 1 : dot > 0d ? -1 : 0;

        ExactInCircleComputations++;
        List<double> exact = Product(Difference(point.X, a.X), Difference(point.X, b.X));
        Expansion.Add(exact, Product(Difference(point.Y, a.Y), Difference(point.Y, b.Y)));
        Expansion.Compress(exact);
        int sign = Expansion.Sign(exact);
        return sign < 0 ? 1 : sign > 0 ? -1 : 0;
    }

    public int InCircleSign(Vec2 a, Vec2 b, Vec2 c, Vec2 point)
    {
        RequireFinite(a, nameof(a));
        RequireFinite(b, nameof(b));
        RequireFinite(c, nameof(c));
        RequireFinite(point, nameof(point));

        int orientation = OrientSign(a, b, c);
        if (orientation == 0) return 0;

        Vec2 ad = a - point;
        Vec2 bd = b - point;
        Vec2 cd = c - point;
        double aLift = ad.X * ad.X + ad.Y * ad.Y;
        double bLift = bd.X * bd.X + bd.Y * bd.Y;
        double cLift = cd.X * cd.X + cd.Y * cd.Y;
        double bc = bd.X * cd.Y - bd.Y * cd.X;
        double ca = cd.X * ad.Y - cd.Y * ad.X;
        double ab = ad.X * bd.Y - ad.Y * bd.X;
        double termA = aLift * bc;
        double termB = bLift * ca;
        double termC = cLift * ab;
        double determinant = termA + termB + termC;
        double signedDeterminant = determinant * orientation;
        double errorBound = 64d * UnitRoundoff *
            (Math.Abs(termA) + Math.Abs(termB) + Math.Abs(termC));
        if (signedDeterminant > errorBound) return 1;
        if (signedDeterminant < -errorBound) return -1;
        if (!AllowExactMath) return Math.Sign(signedDeterminant);

        ExactInCircleComputations++;
        List<double> adx = Difference(a.X, point.X);
        List<double> ady = Difference(a.Y, point.Y);
        List<double> bdx = Difference(b.X, point.X);
        List<double> bdy = Difference(b.Y, point.Y);
        List<double> cdx = Difference(c.X, point.X);
        List<double> cdy = Difference(c.Y, point.Y);

        List<double> exactA = Product(SquareSum(adx, ady), Cross(bdx, bdy, cdx, cdy));
        List<double> exactB = Product(SquareSum(bdx, bdy), Cross(cdx, cdy, adx, ady));
        List<double> exactC = Product(SquareSum(cdx, cdy), Cross(adx, ady, bdx, bdy));
        Expansion.Add(exactA, exactB);
        Expansion.Add(exactA, exactC);
        Expansion.Compress(exactA);
        return Expansion.Sign(exactA) * orientation;
    }

    static List<double> Difference(double left, double right)
    {
        Expansion.TwoSum(left, -right, out double high, out double low);
        var result = new List<double>(2);
        if (low != 0d) result.Add(low);
        if (high != 0d) result.Add(high);
        return result;
    }

    static List<double> Cross(
        List<double> ax,
        List<double> ay,
        List<double> bx,
        List<double> by)
    {
        List<double> result = Product(ax, by);
        List<double> other = Product(ay, bx);
        Expansion.Negate(other);
        Expansion.Add(result, other);
        Expansion.Compress(result);
        return result;
    }

    static List<double> SquareSum(List<double> x, List<double> y)
    {
        List<double> result = Product(x, x);
        Expansion.Add(result, Product(y, y));
        Expansion.Compress(result);
        return result;
    }

    static List<double> Product(List<double> left, List<double> right)
    {
        var result = new List<double>(left);
        Expansion.Mul(result, right);
        Expansion.Compress(result);
        return result;
    }

    static void RequireFinite(Vec2 value, string parameterName)
    {
        if (!double.IsFinite(value.X) || !double.IsFinite(value.Y))
        {
            throw new ArgumentException("Geometry predicate coordinates must be finite.", parameterName);
        }
    }
}
