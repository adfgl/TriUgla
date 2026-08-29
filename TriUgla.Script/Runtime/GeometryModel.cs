using System.Collections.ObjectModel;
using System.Globalization;

namespace TriUgla.Script;

public sealed class GeometryModel
{
    readonly Dictionary<int, ScriptPoint> _points = [];
    readonly Dictionary<int, ScriptLine> _lines = [];
    readonly Dictionary<int, ScriptCurveLoop> _curveLoops = [];
    readonly Dictionary<int, TransfiniteCurveConstraint> _transfiniteCurves = [];
    readonly Dictionary<int, ScriptPlaneSurface> _planeSurfaces = [];
    readonly ReadOnlyDictionary<int, ScriptPoint> _readOnlyPoints;
    readonly ReadOnlyDictionary<int, ScriptLine> _readOnlyLines;
    readonly ReadOnlyDictionary<int, ScriptCurveLoop> _readOnlyCurveLoops;
    readonly ReadOnlyDictionary<int, TransfiniteCurveConstraint> _readOnlyTransfiniteCurves;
    readonly ReadOnlyDictionary<int, ScriptPlaneSurface> _readOnlyPlaneSurfaces;

    public GeometryModel()
    {
        _readOnlyPoints = _points.AsReadOnly();
        _readOnlyLines = _lines.AsReadOnly();
        _readOnlyCurveLoops = _curveLoops.AsReadOnly();
        _readOnlyTransfiniteCurves = _transfiniteCurves.AsReadOnly();
        _readOnlyPlaneSurfaces = _planeSurfaces.AsReadOnly();
    }

    public IReadOnlyDictionary<int, ScriptPoint> Points => _readOnlyPoints;
    public IReadOnlyDictionary<int, ScriptLine> Lines => _readOnlyLines;
    public IReadOnlyDictionary<int, ScriptCurveLoop> CurveLoops => _readOnlyCurveLoops;
    public IReadOnlyDictionary<int, TransfiniteCurveConstraint> TransfiniteCurves => _readOnlyTransfiniteCurves;
    public IReadOnlyDictionary<int, ScriptPlaneSurface> PlaneSurfaces => _readOnlyPlaneSurfaces;

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

    public ScriptLine AddLine(int tag, int startPointTag, int endPointTag)
    {
        if (_lines.ContainsKey(tag))
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
        return line;
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
            if (!_lines.ContainsKey(Math.Abs(orientedTag)))
            {
                throw new InvalidOperationException(
                    $"Curve Loop {tag} references curve {orientedTag}, but Line({Math.Abs(orientedTag)}) is not declared. " +
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
            if (!_lines.ContainsKey(Math.Abs(orientedTag)))
            {
                throw new InvalidOperationException(
                    $"Transfinite Curve references curve {orientedTag}, but Line({Math.Abs(orientedTag)}) is not declared. " +
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
}

public sealed class ScriptPoint(
    int tag,
    double x,
    double y,
    double z,
    double? meshSize) : ScriptObject
{
    public int Tag { get; } = tag;
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Z { get; } = z;
    public double? MeshSize { get; } = meshSize;

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

public sealed class ScriptLine(int tag, ScriptPoint start, ScriptPoint end) : ScriptObject
{
    public int Tag { get; } = tag;
    public ScriptPoint Start { get; } = start;
    public ScriptPoint End { get; } = end;

    public override string ToString() => $"Line({Tag}) = {{{Start.Tag}, {End.Tag}}};";
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

public sealed class ScriptPlaneSurface(int tag, IReadOnlyList<ScriptCurveLoop> curveLoops) : ScriptObject
{
    public int Tag { get; } = tag;
    public IReadOnlyList<ScriptCurveLoop> CurveLoops { get; } = curveLoops;

    public override string ToString()
        => $"Plane Surface({Tag}) = {{{string.Join(", ", CurveLoops.Select(loop => loop.Tag))}}};";
}
