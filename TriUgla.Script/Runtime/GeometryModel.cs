using System.Collections.ObjectModel;
using System.Globalization;

namespace TriUgla.Script;

public sealed class GeometryModel
{
    readonly Dictionary<int, ScriptPoint> _points = [];
    readonly Dictionary<int, ScriptLine> _lines = [];
    readonly Dictionary<int, ScriptCurve> _curves = [];
    readonly Dictionary<int, ScriptCurveLoop> _curveLoops = [];
    readonly Dictionary<int, TransfiniteCurveConstraint> _transfiniteCurves = [];
    readonly Dictionary<int, ScriptPlaneSurface> _planeSurfaces = [];
    readonly Dictionary<string, ScriptPhysicalPointGroup> _physicalPoints = new(StringComparer.Ordinal);
    readonly ReadOnlyDictionary<int, ScriptPoint> _readOnlyPoints;
    readonly ReadOnlyDictionary<int, ScriptLine> _readOnlyLines;
    readonly ReadOnlyDictionary<int, ScriptCurve> _readOnlyCurves;
    readonly ReadOnlyDictionary<int, ScriptCurveLoop> _readOnlyCurveLoops;
    readonly ReadOnlyDictionary<int, TransfiniteCurveConstraint> _readOnlyTransfiniteCurves;
    readonly ReadOnlyDictionary<int, ScriptPlaneSurface> _readOnlyPlaneSurfaces;

    public GeometryModel()
    {
        _readOnlyPoints = _points.AsReadOnly();
        _readOnlyLines = _lines.AsReadOnly();
        _readOnlyCurves = _curves.AsReadOnly();
        _readOnlyCurveLoops = _curveLoops.AsReadOnly();
        _readOnlyTransfiniteCurves = _transfiniteCurves.AsReadOnly();
        _readOnlyPlaneSurfaces = _planeSurfaces.AsReadOnly();
    }

    public IReadOnlyDictionary<int, ScriptPoint> Points => _readOnlyPoints;
    public IReadOnlyDictionary<int, ScriptLine> Lines => _readOnlyLines;
    public IReadOnlyDictionary<int, ScriptCurve> Curves => _readOnlyCurves;
    public IReadOnlyDictionary<int, ScriptCurveLoop> CurveLoops => _readOnlyCurveLoops;
    public IReadOnlyDictionary<int, TransfiniteCurveConstraint> TransfiniteCurves => _readOnlyTransfiniteCurves;
    public IReadOnlyDictionary<int, ScriptPlaneSurface> PlaneSurfaces => _readOnlyPlaneSurfaces;
    public IReadOnlyDictionary<string, ScriptPhysicalPointGroup> PhysicalPoints => _physicalPoints;

    public ScriptPoint AddPoint(int tag, double x, double y, double z, double? meshSize = null)
    {
        if (_points.ContainsKey(tag))
        {
            throw new InvalidOperationException(
                $"Point tag {tag} is already declared. Hint: use a unique point tag or update the existing point.");
        }

        var point = new ScriptPoint(tag, x, y, z, meshSize);
        _points.Add(tag, point);
        return point;
    }

    public ScriptPhysicalPointGroup AddPhysicalPointGroup(string name, IReadOnlyList<int> pointTags)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Physical Point name cannot be empty.");
        if (_physicalPoints.ContainsKey(name))
            throw new InvalidOperationException($"Physical Point '{name}' is already declared.");
        if (pointTags.Count == 0)
            throw new InvalidOperationException($"Physical Point '{name}' requires at least one point tag.");

        ScriptPoint[] points = pointTags.Select(tag => _points.TryGetValue(tag, out ScriptPoint? point)
            ? point
            : throw new InvalidOperationException(
                $"Physical Point '{name}' references Point({tag}), but it is not declared.")).ToArray();
        var group = new ScriptPhysicalPointGroup(name, points);
        _physicalPoints.Add(name, group);
        foreach (ScriptPoint point in points) point.AddPhysicalName(name);
        return group;
    }

    public ScriptLine AddLine(int tag, int startPointTag, int endPointTag)
    {
        if (_curves.ContainsKey(tag))
        {
            throw new InvalidOperationException(
                $"Line tag {tag} is already declared. Hint: use a unique line tag or update the existing line.");
        }

        if (!_points.TryGetValue(startPointTag, out ScriptPoint? start))
        {
            throw new InvalidOperationException(
                $"Line {tag} references point {startPointTag}, but that point is not declared. " +
                $"Hint: declare Point({startPointTag}) before this line.");
        }

        if (!_points.TryGetValue(endPointTag, out ScriptPoint? end))
        {
            throw new InvalidOperationException(
                $"Line {tag} references point {endPointTag}, but that point is not declared. " +
                $"Hint: declare Point({endPointTag}) before this line.");
        }

        var line = new ScriptLine(tag, start, end);
        _lines.Add(tag, line);
        _curves.Add(tag, line);
        return line;
    }

    public ScriptCurve AddSpline(int tag, IReadOnlyList<int> pointTags, ScriptCurveKind kind)
    {
        if (_curves.ContainsKey(tag))
        {
            throw new InvalidOperationException(
                $"Curve tag {tag} is already declared. Hint: curve tags are shared by Line, Spline, BSpline, Bezier and Circle.");
        }

        int minimum = kind == ScriptCurveKind.Circle ? 3 : 2;
        if (pointTags.Count < minimum)
        {
            throw new InvalidOperationException(
                $"{kind} {tag} expects at least {minimum} point tags, but received {pointTags.Count}.");
        }

        ScriptPoint[] points = pointTags
            .Select(pointTag => _points.TryGetValue(pointTag, out ScriptPoint? point)
                ? point
                : throw new InvalidOperationException(
                    $"{kind} {tag} references Point({pointTag}), but it is not declared. " +
                    $"Hint: declare Point({pointTag}) before this curve."))
            .ToArray();
        ScriptCurve curve = kind switch
        {
            ScriptCurveKind.Spline => new ScriptSpline(tag, points),
            ScriptCurveKind.BSpline => new ScriptBSpline(tag, points),
            ScriptCurveKind.Bezier => new ScriptBezier(tag, points),
            ScriptCurveKind.Circle => new ScriptCircle(tag, points[0], points[1], points[2]),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        _curves.Add(tag, curve);
        return curve;
    }

    public ScriptCurveLoop AddCurveLoop(int tag, IReadOnlyList<int> orientedCurveTags)
    {
        if (_curveLoops.ContainsKey(tag))
        {
            throw new InvalidOperationException(
                $"Curve Loop tag {tag} is already declared. Hint: use a unique curve loop tag.");
        }

        if (orientedCurveTags.Count == 0)
        {
            throw new InvalidOperationException(
                $"Curve Loop {tag} must contain at least one curve tag.");
        }

        foreach (int orientedTag in orientedCurveTags)
        {
            if (!_curves.ContainsKey(Math.Abs(orientedTag)))
            {
                throw new InvalidOperationException(
                    $"Curve Loop {tag} references curve {orientedTag}, but Curve({Math.Abs(orientedTag)}) is not declared. " +
                    $"Hint: declare the curve before the loop.");
            }
        }

        var loop = new ScriptCurveLoop(tag, orientedCurveTags.ToArray());
        _curveLoops.Add(tag, loop);
        return loop;
    }

    public TransfiniteCurveConstraint SetTransfiniteCurves(
        IReadOnlyList<int> orientedCurveTags,
        int nodeCount,
        TransfiniteCurveDistribution distribution,
        double coefficient)
    {
        if (orientedCurveTags.Count == 0)
        {
            throw new InvalidOperationException("Transfinite Curve requires at least one curve tag.");
        }

        foreach (int orientedTag in orientedCurveTags)
        {
            if (!_curves.ContainsKey(Math.Abs(orientedTag)))
            {
                throw new InvalidOperationException(
                    $"Transfinite Curve references curve {orientedTag}, but Curve({Math.Abs(orientedTag)}) is not declared. " +
                    "Hint: declare each curve before applying a transfinite constraint.");
            }
        }

        var constraint = new TransfiniteCurveConstraint(
            orientedCurveTags.ToArray(),
            nodeCount,
            distribution,
            coefficient);
        foreach (int orientedTag in orientedCurveTags)
        {
            _transfiniteCurves[Math.Abs(orientedTag)] = constraint;
        }

        return constraint;
    }

    public ScriptPlaneSurface AddPlaneSurface(int tag, IReadOnlyList<int> curveLoopTags)
    {
        if (_planeSurfaces.ContainsKey(tag))
        {
            throw new InvalidOperationException(
                $"Plane Surface tag {tag} is already declared. Hint: use a unique surface tag.");
        }

        if (curveLoopTags.Count == 0)
        {
            throw new InvalidOperationException(
                $"Plane Surface {tag} requires at least one curve loop. " +
                "Hint: the first loop defines the exterior boundary and additional loops define holes.");
        }

        var loops = new ScriptCurveLoop[curveLoopTags.Count];
        for (int index = 0; index < curveLoopTags.Count; index++)
        {
            int loopTag = curveLoopTags[index];
            if (!_curveLoops.TryGetValue(loopTag, out ScriptCurveLoop? loop))
            {
                throw new InvalidOperationException(
                    $"Plane Surface {tag} references Curve Loop({loopTag}), but it is not declared. " +
                    $"Hint: declare Curve Loop({loopTag}) before the surface.");
            }

            loops[index] = loop;
        }

        var planeSurface = new ScriptPlaneSurface(tag, loops);
        _planeSurfaces.Add(tag, planeSurface);
        return planeSurface;
    }

    public ScriptPlaneSurface EmbedCurvesInSurface(
        IReadOnlyList<int> curveTags,
        int surfaceTag)
    {
        if (!_planeSurfaces.TryGetValue(surfaceTag, out ScriptPlaneSurface? surface))
        {
            throw new InvalidOperationException(
                $"Cannot embed curves in Plane Surface({surfaceTag}) because it is not declared. " +
                $"Hint: declare Plane Surface({surfaceTag}) before the embedding constraint.");
        }

        if (curveTags.Count == 0)
        {
            throw new InvalidOperationException(
                "Curve In Surface requires at least one curve tag.");
        }

        foreach (int curveTag in curveTags)
        {
            if (!_curves.ContainsKey(curveTag))
            {
                throw new InvalidOperationException(
                    $"Cannot embed Curve({curveTag}) because it is not declared. " +
                    "Hint: declare every embedded curve first.");
            }
        }

        surface.AddEmbeddedCurves(curveTags);
        return surface;
    }

    public IReadOnlyList<ScriptMeshNode> GetTransfiniteNodes()
    {
        var nodes = new List<ScriptMeshNode>();
        foreach ((int curveTag, TransfiniteCurveConstraint constraint) in _transfiniteCurves.OrderBy(item => item.Key))
        {
            ScriptCurve curve = _curves[curveTag];
            int orientedTag = constraint.OrientedCurveTags.First(tag => Math.Abs(tag) == curveTag);
            double[] fractions = TransfiniteFractions(constraint);
            for (int index = 1; index < fractions.Length - 1; index++)
            {
                double fraction = orientedTag < 0 ? 1d - fractions[index] : fractions[index];
                CurvePosition position = curve.EvaluateByArcFraction(fraction);
                nodes.Add(new ScriptMeshNode(
                    curveTag,
                    index,
                    position.X,
                    position.Y,
                    position.Z));
            }
        }

        return nodes;
    }

    static double[] TransfiniteFractions(TransfiniteCurveConstraint constraint)
    {
        int segmentCount = constraint.NodeCount - 1;
        var logarithmicWeights = new double[segmentCount];
        double logCoefficient = Math.Log(constraint.Coefficient);
        for (int segment = 0; segment < segmentCount; segment++)
        {
            logarithmicWeights[segment] = constraint.Distribution switch
            {
                TransfiniteCurveDistribution.Progression => segment * logCoefficient,
                TransfiniteCurveDistribution.Bump =>
                    Math.Min(segment, segmentCount - 1 - segment) * Math.Abs(logCoefficient),
                _ => 0d
            };
        }

        double maximum = logarithmicWeights.Max();
        double[] weights = logarithmicWeights.Select(weight => Math.Exp(weight - maximum)).ToArray();
        double total = weights.Sum();
        var fractions = new double[constraint.NodeCount];
        double cumulative = 0d;
        for (int node = 1; node < fractions.Length; node++)
        {
            cumulative += weights[node - 1];
            fractions[node] = cumulative / total;
        }

        fractions[^1] = 1d;
        return fractions;
    }
}

public sealed class ScriptPoint(
    int tag,
    double x,
    double y,
    double z,
    double? meshSize) : ScriptObject
{
    readonly List<string> _physicalNames = [];
    public int Tag { get; } = tag;
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Z { get; } = z;
    public double? MeshSize { get; } = meshSize;
    public IReadOnlyList<string> PhysicalNames => _physicalNames;

    internal void AddPhysicalName(string name) => _physicalNames.Add(name);

    public override string ToString()
    {
        string values = string.Join(", ", new[] { X, Y, Z }
            .Select(value => value.ToString(CultureInfo.InvariantCulture)));
        if (MeshSize is double size)
        {
            values += $", {size.ToString(CultureInfo.InvariantCulture)}";
        }

        return $"Point({Tag}) = {{{values}}};";
    }
}

public sealed class ScriptPhysicalPointGroup(string name, IReadOnlyList<ScriptPoint> points) : ScriptObject
{
    public string Name { get; } = name;
    public IReadOnlyList<ScriptPoint> Points { get; } = points;

    public override string ToString()
        => $"Physical Point(\"{Name}\") = {{{string.Join(", ", Points.Select(point => point.Tag))}}};";
}

public readonly record struct CurvePosition(double X, double Y, double Z)
{
    public static CurvePosition Lerp(CurvePosition start, CurvePosition end, double amount)
        => new(
            start.X + (end.X - start.X) * amount,
            start.Y + (end.Y - start.Y) * amount,
            start.Z + (end.Z - start.Z) * amount);
}

public enum ScriptCurveKind
{
    Line,
    Spline,
    BSpline,
    Bezier,
    Circle
}

public abstract class ScriptCurve(int tag, ScriptCurveKind kind) : ScriptObject
{
    public int Tag { get; } = tag;
    public ScriptCurveKind Kind { get; } = kind;
    public abstract ScriptPoint Start { get; }
    public abstract ScriptPoint End { get; }
    public abstract CurvePosition Evaluate(double parameter);

    public virtual IReadOnlyList<CurvePosition> Tessellate(int segments = 64)
        => Enumerable.Range(0, Math.Max(1, segments) + 1)
            .Select(index => Evaluate((double)index / Math.Max(1, segments)))
            .ToArray();

    public CurvePosition EvaluateByArcFraction(double fraction)
    {
        fraction = Math.Clamp(fraction, 0d, 1d);
        const int samples = 256;
        var positions = new CurvePosition[samples + 1];
        var lengths = new double[samples + 1];
        positions[0] = Evaluate(0d);
        for (int index = 1; index <= samples; index++)
        {
            positions[index] = Evaluate((double)index / samples);
            lengths[index] = lengths[index - 1] + Distance(positions[index - 1], positions[index]);
        }

        double target = lengths[^1] * fraction;
        int upper = Array.BinarySearch(lengths, target);
        if (upper >= 0) return positions[upper];
        upper = ~upper;
        if (upper <= 0) return positions[0];
        if (upper >= lengths.Length) return positions[^1];
        double span = lengths[upper] - lengths[upper - 1];
        double local = span <= double.Epsilon ? 0d : (target - lengths[upper - 1]) / span;
        return CurvePosition.Lerp(positions[upper - 1], positions[upper], local);
    }

    protected static CurvePosition Position(ScriptPoint point)
        => new(point.X, point.Y, point.Z);

    static double Distance(CurvePosition left, CurvePosition right)
        => Math.Sqrt(
            Math.Pow(right.X - left.X, 2) +
            Math.Pow(right.Y - left.Y, 2) +
            Math.Pow(right.Z - left.Z, 2));
}

public sealed class ScriptLine(int tag, ScriptPoint start, ScriptPoint end)
    : ScriptCurve(tag, ScriptCurveKind.Line)
{
    public override ScriptPoint Start { get; } = start;
    public override ScriptPoint End { get; } = end;

    public override CurvePosition Evaluate(double parameter)
        => CurvePosition.Lerp(Position(Start), Position(End), Math.Clamp(parameter, 0d, 1d));

    public override IReadOnlyList<CurvePosition> Tessellate(int segments = 1)
        => [Position(Start), Position(End)];

    public override string ToString() => $"Line({Tag}) = {{{Start.Tag}, {End.Tag}}};";
}

public abstract class ScriptControlPointCurve(
    int tag,
    ScriptCurveKind kind,
    IReadOnlyList<ScriptPoint> controlPoints)
    : ScriptCurve(tag, kind)
{
    public IReadOnlyList<ScriptPoint> ControlPoints { get; } = controlPoints;
    public override ScriptPoint Start => ControlPoints[0];
    public override ScriptPoint End => ControlPoints[^1];
    public override string ToString()
        => $"{Kind}({Tag}) = {{{string.Join(", ", ControlPoints.Select(point => point.Tag))}}};";
}

public sealed class ScriptSpline(int tag, IReadOnlyList<ScriptPoint> points)
    : ScriptControlPointCurve(tag, ScriptCurveKind.Spline, points)
{
    public override CurvePosition Evaluate(double parameter)
    {
        int segmentCount = ControlPoints.Count - 1;
        double scaled = Math.Clamp(parameter, 0d, 1d) * segmentCount;
        int segment = Math.Min((int)scaled, segmentCount - 1);
        double t = scaled - segment;
        CurvePosition p0 = Position(ControlPoints[Math.Max(0, segment - 1)]);
        CurvePosition p1 = Position(ControlPoints[segment]);
        CurvePosition p2 = Position(ControlPoints[segment + 1]);
        CurvePosition p3 = Position(ControlPoints[Math.Min(ControlPoints.Count - 1, segment + 2)]);
        return CatmullRom(p0, p1, p2, p3, t);
    }

    static CurvePosition CatmullRom(
        CurvePosition p0,
        CurvePosition p1,
        CurvePosition p2,
        CurvePosition p3,
        double t)
    {
        double t2 = t * t;
        double t3 = t2 * t;
        return new CurvePosition(
            .5 * (2 * p1.X + (-p0.X + p2.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3),
            .5 * (2 * p1.Y + (-p0.Y + p2.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3),
            .5 * (2 * p1.Z + (-p0.Z + p2.Z) * t + (2 * p0.Z - 5 * p1.Z + 4 * p2.Z - p3.Z) * t2 + (-p0.Z + 3 * p1.Z - 3 * p2.Z + p3.Z) * t3));
    }
}

public sealed class ScriptBezier(int tag, IReadOnlyList<ScriptPoint> points)
    : ScriptControlPointCurve(tag, ScriptCurveKind.Bezier, points)
{
    public override CurvePosition Evaluate(double parameter)
    {
        double t = Math.Clamp(parameter, 0d, 1d);
        CurvePosition[] work = ControlPoints.Select(Position).ToArray();
        for (int level = work.Length - 1; level > 0; level--)
        {
            for (int index = 0; index < level; index++)
            {
                work[index] = CurvePosition.Lerp(work[index], work[index + 1], t);
            }
        }

        return work[0];
    }
}

public sealed class ScriptBSpline(int tag, IReadOnlyList<ScriptPoint> points)
    : ScriptControlPointCurve(tag, ScriptCurveKind.BSpline, points)
{
    public override CurvePosition Evaluate(double parameter)
    {
        int degree = Math.Min(3, ControlPoints.Count - 1);
        int n = ControlPoints.Count - 1;
        int knotCount = n + degree + 2;
        var knots = new double[knotCount];
        for (int index = 0; index < knotCount; index++)
        {
            knots[index] = index <= degree
                ? 0d
                : index >= knotCount - degree - 1
                    ? 1d
                    : (double)(index - degree) / (knotCount - 2 * degree - 1);
        }

        double t = Math.Clamp(parameter, 0d, 1d);
        int span = t >= 1d
            ? n
            : Enumerable.Range(degree, n - degree + 1).First(index => t >= knots[index] && t < knots[index + 1]);
        CurvePosition[] work = Enumerable.Range(0, degree + 1)
            .Select(index => Position(ControlPoints[span - degree + index]))
            .ToArray();
        for (int level = 1; level <= degree; level++)
        {
            for (int index = degree; index >= level; index--)
            {
                int knotIndex = span - degree + index;
                double denominator = knots[knotIndex + degree - level + 1] - knots[knotIndex];
                double alpha = denominator == 0d ? 0d : (t - knots[knotIndex]) / denominator;
                work[index] = CurvePosition.Lerp(work[index - 1], work[index], alpha);
            }
        }

        return work[degree];
    }
}

public sealed class ScriptCircle : ScriptCurve
{
    readonly CurvePosition _center;
    readonly CurvePosition _radial;
    readonly CurvePosition _tangent;
    readonly double _angle;

    public ScriptCircle(int tag, ScriptPoint start, ScriptPoint center, ScriptPoint end)
        : base(tag, ScriptCurveKind.Circle)
    {
        Start = start;
        Center = center;
        End = end;
        _center = Position(center);
        CurvePosition startVector = Subtract(Position(start), _center);
        CurvePosition endVector = Subtract(Position(end), _center);
        double startRadius = Length(startVector);
        double endRadius = Length(endVector);
        if (startRadius <= 0d || Math.Abs(startRadius - endRadius) > Math.Max(startRadius, endRadius) * 1e-8)
        {
            throw new InvalidOperationException(
                $"Circle {tag} start and end points must have the same non-zero distance from its center.");
        }

        _radial = Scale(startVector, 1d / startRadius);
        double cosine = Math.Clamp(Dot(startVector, endVector) / (startRadius * endRadius), -1d, 1d);
        _angle = Math.Acos(cosine);
        if (_angle <= 1e-10 || _angle >= Math.PI - 1e-10)
        {
            throw new InvalidOperationException(
                $"Circle {tag} must define a non-collinear arc strictly smaller than Pi.");
        }

        CurvePosition perpendicular = Subtract(Scale(endVector, 1d / endRadius), Scale(_radial, cosine));
        _tangent = Scale(perpendicular, 1d / Length(perpendicular));
        Radius = startRadius;
    }

    public override ScriptPoint Start { get; }
    public ScriptPoint Center { get; }
    public override ScriptPoint End { get; }
    public double Radius { get; }

    public override CurvePosition Evaluate(double parameter)
    {
        double angle = _angle * Math.Clamp(parameter, 0d, 1d);
        return Add(_center, Scale(Add(Scale(_radial, Math.Cos(angle)), Scale(_tangent, Math.Sin(angle))), Radius));
    }

    public override string ToString() => $"Circle({Tag}) = {{{Start.Tag}, {Center.Tag}, {End.Tag}}};";

    static CurvePosition Add(CurvePosition a, CurvePosition b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    static CurvePosition Subtract(CurvePosition a, CurvePosition b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    static CurvePosition Scale(CurvePosition value, double factor) => new(value.X * factor, value.Y * factor, value.Z * factor);
    static double Dot(CurvePosition a, CurvePosition b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    static double Length(CurvePosition value) => Math.Sqrt(Dot(value, value));
}

public sealed class ScriptCurveLoop(int tag, IReadOnlyList<int> orientedCurveTags) : ScriptObject
{
    public int Tag { get; } = tag;
    public IReadOnlyList<int> OrientedCurveTags { get; } = orientedCurveTags;

    public override string ToString()
        => $"Curve Loop({Tag}) = {{{string.Join(", ", OrientedCurveTags)}}};";
}

public enum TransfiniteCurveDistribution
{
    Progression,
    Bump
}

public sealed class TransfiniteCurveConstraint(
    IReadOnlyList<int> orientedCurveTags,
    int nodeCount,
    TransfiniteCurveDistribution distribution,
    double coefficient) : ScriptObject
{
    public IReadOnlyList<int> OrientedCurveTags { get; } = orientedCurveTags;
    public int NodeCount { get; } = nodeCount;
    public TransfiniteCurveDistribution Distribution { get; } = distribution;
    public double Coefficient { get; } = coefficient;

    public override string ToString()
    {
        string option = Distribution == TransfiniteCurveDistribution.Progression && Coefficient == 1d
            ? string.Empty
            : $" Using {Distribution} {Coefficient.ToString(CultureInfo.InvariantCulture)}";
        return $"Transfinite Curve {{{string.Join(", ", OrientedCurveTags)}}} = {NodeCount}{option};";
    }
}

public sealed class ScriptPlaneSurface : ScriptObject
{
    readonly HashSet<int> _embeddedCurveTags = [];

    public ScriptPlaneSurface(int tag, IReadOnlyList<ScriptCurveLoop> curveLoops)
    {
        Tag = tag;
        CurveLoops = curveLoops;
    }

    public int Tag { get; }
    public IReadOnlyList<ScriptCurveLoop> CurveLoops { get; }
    public IReadOnlySet<int> EmbeddedCurveTags => _embeddedCurveTags;

    internal void AddEmbeddedCurves(IEnumerable<int> curveTags)
        => _embeddedCurveTags.UnionWith(curveTags);

    public override string ToString()
        => $"Plane Surface({Tag}) = {{{string.Join(", ", CurveLoops.Select(loop => loop.Tag))}}};";
}

public sealed record ScriptMeshNode(
    int CurveTag,
    int CurveNodeIndex,
    double X,
    double Y,
    double Z);
