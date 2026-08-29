using System.Globalization;

namespace TriUgla.Script;

public static class ScriptMesher
{
    const int CurvedSegmentCount = 32;
    const int MaximumSteinerNodes = 10_000;
    const int CoordinateDigits = 6;

    public static ScriptMeshResult Generate(
        GeometryModel geometry,
        MeshScriptModel options,
        int dimension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(options);
        if (dimension != 2)
        {
            throw new InvalidOperationException(
                $"Mesh {dimension} is not supported by the 2D constrained mesher. Hint: use 'Mesh 2;'.");
        }
        if (geometry.PlaneSurfaces.Count == 0)
        {
            throw new InvalidOperationException(
                "Mesh 2 requires at least one Plane Surface. Hint: declare Curve Loops and a Plane Surface before 'Mesh 2;'.");
        }

        var faces = new List<ScriptMeshFace>();
        int steinerNodes = 0;
        foreach (ScriptPlaneSurface surface in geometry.PlaneSurfaces.Values.OrderBy(surface => surface.Tag))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                PreparedSurface prepared = PrepareSurface(geometry, surface, cancellationToken);
                int inserted = Refine(
                    prepared.Mesher,
                    geometry,
                    options,
                    prepared.Positions,
                    cancellationToken);
                SurfaceMesh result = CompleteSurface(
                    surface.Tag,
                    prepared.Mesher,
                    inserted);
                faces.AddRange(result.Faces);
                steinerNodes += result.SteinerNodes;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Plane Surface({surface.Tag}) could not be meshed: {exception.Message}", exception);
            }
        }
        return new ScriptMeshResult(
            faces,
            steinerNodes,
            ScriptMeshMetrics.Calculate(faces));
    }

    public static async ValueTask<ScriptMeshResult> GenerateAsync(
        GeometryModel geometry,
        MeshScriptModel options,
        int dimension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(options);
        if (dimension != 2)
            throw new InvalidOperationException(
                $"Mesh {dimension} is not supported by the 2D constrained mesher. Hint: use 'Mesh 2;'.");
        if (geometry.PlaneSurfaces.Count == 0)
            throw new InvalidOperationException(
                "Mesh 2 requires at least one Plane Surface. Hint: declare Curve Loops and a Plane Surface before 'Mesh 2;'.");

        var faces = new List<ScriptMeshFace>();
        int steinerNodes = 0;
        foreach (ScriptPlaneSurface surface in geometry.PlaneSurfaces.Values.OrderBy(surface => surface.Tag))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            try
            {
                PreparedSurface prepared = PrepareSurface(geometry, surface, cancellationToken);
                await Task.Yield();
                int inserted = await RefineAsync(
                    prepared.Mesher,
                    geometry,
                    options,
                    prepared.Positions,
                    cancellationToken);
                SurfaceMesh result = CompleteSurface(
                    surface.Tag,
                    prepared.Mesher,
                    inserted);
                faces.AddRange(result.Faces);
                steinerNodes += result.SteinerNodes;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Plane Surface({surface.Tag}) could not be meshed: {exception.Message}", exception);
            }
        }
        return new ScriptMeshResult(
            faces,
            steinerNodes,
            ScriptMeshMetrics.Calculate(faces));
    }

    static PreparedSurface PrepareSurface(
        GeometryModel geometry,
        ScriptPlaneSurface surface,
        CancellationToken cancellationToken)
    {
        List<CurvePosition> positions = surface.CurveLoops
            .SelectMany(loop => LoopPositions(geometry, loop))
            .Concat(surface.EmbeddedCurveTags.SelectMany(tag => CurvePositions(geometry, tag)))
            .Select(Normalize)
            .ToList();
        if (positions.Count < 3) throw new InvalidOperationException("the surface has fewer than three boundary points.");

        Vec2 min = new(positions.Min(position => position.X), positions.Min(position => position.Y));
        Vec2 max = new(positions.Max(position => position.X), positions.Max(position => position.Y));
        if (min == max) throw new InvalidOperationException("the surface bounds have zero size.");
        Vec2 padding = Vec2.Make(Math.Max((max - min).Length * .1, 1e-6));
        var mesher = new Mesher(min - padding, max + padding);

        var nodes = new Dictionary<Vec2, Node>();
        Node GetNode(CurvePosition position)
        {
            CurvePosition normalized = Normalize(position);
            var key = new Vec2(normalized.X, normalized.Y);
            if (nodes.TryGetValue(key, out Node? existing))
            {
                if (Math.Abs(existing.Data.Elevation - normalized.Z) > 1e-6)
                {
                    throw new InvalidOperationException(
                        $"points at XY ({key.X}, {key.Y}) have conflicting Z elevations " +
                        $"{existing.Data.Elevation} and {normalized.Z}.");
                }
                return existing;
            }
            InsertNodeResult insertion = mesher.Insert(key);
            Node node = insertion.Node ?? throw new InvalidOperationException(
                $"point ({normalized.X}, {normalized.Y}) lies outside the meshing super structure.");
            node.Data = new NodeData(normalized.Z, 0d);
            nodes.Add(key, node);
            return node;
        }

        foreach (ScriptCurveLoop scriptLoop in surface.CurveLoops)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Node[] loopNodes = LoopPositions(geometry, scriptLoop).Select(GetNode).ToArray();
            var loop = new Loop(loopNodes, $"Curve Loop({scriptLoop.Tag})");
            if (!mesher.TryInsertLoop(loop, out string? reason))
                throw new InvalidOperationException(reason);
        }

        foreach (int curveTag in surface.EmbeddedCurveTags.Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Node[] curveNodes = CurvePositions(geometry, curveTag).Select(GetNode).ToArray();
            var spans = curveNodes.Zip(curveNodes.Skip(1), (from, to) => new ConstraintSpan(from, to));
            var constraint = new Constraint(spans: spans, name: $"embedded Curve({curveTag})");
            if (!mesher.TryInsertConstraint(constraint, out string? reason))
                throw new InvalidOperationException(reason);
        }

        return new PreparedSurface(mesher, positions);
    }

    static SurfaceMesh CompleteSurface(
        int surfaceTag,
        Mesher mesher,
        int steinerNodes)
    {
        ScriptMeshFace[] faces = mesher.Traversal.Faces()
            .Where(face => !face.Dead)
            .Select(face => new ScriptMeshFace(
                surfaceTag,
                face.Kind,
                mesher.SuperStructure!.SuperFace(face),
                face.Edges.Select(edge => new ScriptMeshVertex(
                    edge.NodeStart.Position.X,
                    edge.NodeStart.Position.Y,
                    edge.NodeStart.Data.Elevation)).ToArray()))
            .ToArray();
        return new SurfaceMesh(faces, steinerNodes);
    }

    static int Refine(
        Mesher mesher,
        GeometryModel geometry,
        MeshScriptModel options,
        IReadOnlyList<CurvePosition> positions,
        CancellationToken cancellationToken)
    {
        (FaceRanker ranker, int budget) = RefinementPlan(geometry, options, positions);
        return mesher.Refine(
            ranker,
            new RefineSettings(
                budget,
                8,
                1e-4,
                Option(options, "RefinementContinueOnStagnation") != 0d),
            cancellationToken);
    }

    static (FaceRanker Ranker, int Budget) RefinementPlan(
        GeometryModel geometry,
        MeshScriptModel options,
        IReadOnlyList<CurvePosition> positions)
    {
        double? maximum = Option(options, "CharacteristicLengthMax");
        if (maximum is null && Option(options, "CharacteristicLengthFromPoints") != 0d)
        {
            maximum = positions
                .Select(position => FindPointMeshSize(position, geometry))
                .Where(size => size is > 0d)
                .DefaultIfEmpty()
                .Max();
            if (maximum == 0d) maximum = null;
        }

        var ranker = new FaceRanker();
        ranker.Angle.Weight = 0d;
        ranker.VertexArea.Weight = 0d;
        ranker.Edge.Weight = maximum is > 0d ? 1d : 0d;
        if (maximum is > 0d) ranker.Edge.MaxEdgeLength = maximum.Value;
        int budget = maximum is > 0d ? MaximumSteinerNodes : 0;
        return (ranker, budget);
    }

    static async ValueTask<int> RefineAsync(
        Mesher mesher,
        GeometryModel geometry,
        MeshScriptModel options,
        IReadOnlyList<CurvePosition> positions,
        CancellationToken cancellationToken)
    {
        (FaceRanker ranker, int budget) = RefinementPlan(geometry, options, positions);
        return await mesher.RefineAsync(
            ranker,
            new RefineSettings(
                budget,
                8,
                1e-4,
                Option(options, "RefinementContinueOnStagnation") != 0d),
            cancellationToken);
    }

    static double? FindPointMeshSize(CurvePosition position, GeometryModel geometry)
        => geometry.Points.Values.FirstOrDefault(point =>
            point.X == position.X && point.Y == position.Y && point.Z == position.Z)?.MeshSize;

    static double? Option(MeshScriptModel mesh, string name)
        => mesh.Options.TryGetValue(name, out double value) ? value : null;

    static IReadOnlyList<CurvePosition> LoopPositions(GeometryModel geometry, ScriptCurveLoop loop)
    {
        var result = new List<CurvePosition>();
        foreach (int orientedTag in loop.OrientedCurveTags)
        {
            IReadOnlyList<CurvePosition> segment = CurvePositions(geometry, Math.Abs(orientedTag));
            IEnumerable<CurvePosition> oriented = orientedTag < 0 ? segment.Reverse() : segment;
            CurvePosition[] points = oriented.ToArray();
            if (result.Count > 0 && !Same(result[^1], points[0]))
            {
                CurvePosition expected = Normalize(result[^1]);
                CurvePosition found = Normalize(points[0]);
                throw new InvalidOperationException(
                    $"Curve Loop({loop.Tag}) is disconnected between curves; " +
                    $"expected ({expected.X}, {expected.Y}, {expected.Z}) but found " +
                    $"({found.X}, {found.Y}, {found.Z}).");
            }
            result.AddRange(result.Count == 0 ? points : points.Skip(1));
        }
        if (result.Count > 1 && Same(result[0], result[^1])) result.RemoveAt(result.Count - 1);
        if (result.Count < 3 || !SameCurveEnds(geometry, loop))
            throw new InvalidOperationException($"Curve Loop({loop.Tag}) is not closed.");
        return result;
    }

    static bool SameCurveEnds(GeometryModel geometry, ScriptCurveLoop loop)
    {
        int firstTag = loop.OrientedCurveTags[0];
        int lastTag = loop.OrientedCurveTags[^1];
        ScriptCurve first = geometry.Curves[Math.Abs(firstTag)];
        ScriptCurve last = geometry.Curves[Math.Abs(lastTag)];
        ScriptPoint start = firstTag < 0 ? first.End : first.Start;
        ScriptPoint end = lastTag < 0 ? last.Start : last.End;
        return Same(
            new CurvePosition(start.X, start.Y, start.Z),
            new CurvePosition(end.X, end.Y, end.Z));
    }

    static IReadOnlyList<CurvePosition> CurvePositions(GeometryModel geometry, int curveTag)
    {
        ScriptCurve curve = geometry.Curves[curveTag];
        if (geometry.TransfiniteCurves.TryGetValue(curveTag, out TransfiniteCurveConstraint? constraint))
        {
            return GeometryModel.TransfiniteFractions(constraint)
                .Select(curve.EvaluateByArcFraction)
                .ToArray();
        }
        return curve.Tessellate(curve.Kind == ScriptCurveKind.Line ? 1 : CurvedSegmentCount);
    }

    static bool Same(CurvePosition left, CurvePosition right)
        => Normalize(left) == Normalize(right);

    // Script curves are sampled with floating-point trigonometry and interpolation.
    // Their mathematically identical endpoints can therefore differ in the final bits
    // (for example 0.19999999999999996 versus 0.2). Six decimal places is the public
    // meshing precision: normalize here so connectivity and node identity use the same
    // deterministic coordinates while retaining microunit-scale geometric detail.
    static CurvePosition Normalize(CurvePosition position)
        => new(
            Normalize(position.X),
            Normalize(position.Y),
            Normalize(position.Z));

    static double Normalize(double value)
        => Math.Round(value, CoordinateDigits, MidpointRounding.AwayFromZero);

    readonly record struct PreparedSurface(
        Mesher Mesher,
        IReadOnlyList<CurvePosition> Positions);
    readonly record struct SurfaceMesh(IReadOnlyList<ScriptMeshFace> Faces, int SteinerNodes);
}

public sealed record ScriptMeshResult(
    IReadOnlyList<ScriptMeshFace> Faces,
    int SteinerNodes,
    ScriptMeshMetrics Metrics);
public sealed record ScriptMeshFace(
    int SurfaceTag,
    FaceKind Kind,
    bool ContainsSuperStructure,
    IReadOnlyList<ScriptMeshVertex> Vertices);
public readonly record struct ScriptMeshVertex(double X, double Y, double Z);

public sealed record ScriptMeshMetrics(
    ScriptMeshMetric Angle,
    ScriptMeshMetric EdgeLength,
    ScriptMeshMetric FaceArea,
    int DegenerateFaces)
{
    internal static ScriptMeshMetrics Calculate(IEnumerable<ScriptMeshFace> allFaces)
    {
        ScriptMeshFace[] faces = allFaces
            .Where(face => face.Kind == FaceKind.Island && !face.ContainsSuperStructure)
            .ToArray();
        var angles = new List<double>(faces.Length * 3);
        var areas = new List<double>(faces.Length);
        var edgeLengths = new List<double>();
        var edges = new HashSet<(int Surface, ScriptMeshVertex A, ScriptMeshVertex B)>();

        foreach (ScriptMeshFace face in faces)
        {
            if (face.Vertices.Count != 3) continue;
            ScriptMeshVertex a = face.Vertices[0];
            ScriptMeshVertex b = face.Vertices[1];
            ScriptMeshVertex c = face.Vertices[2];
            areas.Add(TriangleArea(a, b, c));
            angles.Add(AngleAt(a, b, c));
            angles.Add(AngleAt(b, c, a));
            angles.Add(AngleAt(c, a, b));

            AddEdge(face.SurfaceTag, a, b, edges, edgeLengths);
            AddEdge(face.SurfaceTag, b, c, edges, edgeLengths);
            AddEdge(face.SurfaceTag, c, a, edges, edgeLengths);
        }

        return new ScriptMeshMetrics(
            ScriptMeshMetric.From(angles),
            ScriptMeshMetric.From(edgeLengths),
            ScriptMeshMetric.From(areas),
            areas.Count(area => !double.IsFinite(area) || area <= 1e-15));
    }

    public override string ToString()
        => $"""
            Mesh metrics
            ------------
            Angle       : {Angle.Format("°")}
            Edge length : {EdgeLength.Format()}
            Face area   : {FaceArea.Format()}
            Degenerate  : {DegenerateFaces} faces
            """;

    static void AddEdge(
        int surface,
        ScriptMeshVertex first,
        ScriptMeshVertex second,
        HashSet<(int Surface, ScriptMeshVertex A, ScriptMeshVertex B)> edges,
        List<double> lengths)
    {
        (ScriptMeshVertex a, ScriptMeshVertex b) = Compare(first, second) <= 0
            ? (first, second)
            : (second, first);
        if (edges.Add((surface, a, b))) lengths.Add(Distance(a, b));
    }

    static int Compare(ScriptMeshVertex left, ScriptMeshVertex right)
    {
        int x = left.X.CompareTo(right.X);
        if (x != 0) return x;
        int y = left.Y.CompareTo(right.Y);
        return y != 0 ? y : left.Z.CompareTo(right.Z);
    }

    static double AngleAt(ScriptMeshVertex center, ScriptMeshVertex first, ScriptMeshVertex second)
    {
        (double x, double y, double z) u = Subtract(first, center);
        (double x, double y, double z) v = Subtract(second, center);
        double denominator = Length(u) * Length(v);
        if (denominator == 0d) return 0d;
        double cosine = Math.Clamp((u.x * v.x + u.y * v.y + u.z * v.z) / denominator, -1d, 1d);
        return Math.Acos(cosine) * 180d / Math.PI;
    }

    static double TriangleArea(ScriptMeshVertex a, ScriptMeshVertex b, ScriptMeshVertex c)
    {
        (double x, double y, double z) u = Subtract(b, a);
        (double x, double y, double z) v = Subtract(c, a);
        double x = u.y * v.z - u.z * v.y;
        double y = u.z * v.x - u.x * v.z;
        double z = u.x * v.y - u.y * v.x;
        return Math.Sqrt(x * x + y * y + z * z) * .5d;
    }

    static double Distance(ScriptMeshVertex a, ScriptMeshVertex b)
        => Length(Subtract(a, b));

    static (double x, double y, double z) Subtract(ScriptMeshVertex a, ScriptMeshVertex b)
        => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    static double Length((double x, double y, double z) value)
        => Math.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
}

public readonly record struct ScriptMeshMetric(int Count, double Min, double Average, double Max)
{
    internal static ScriptMeshMetric From(IReadOnlyCollection<double> values)
        => values.Count == 0
            ? default
            : new(values.Count, values.Min(), values.Average(), values.Max());

    public string Format(string? unit = null)
    {
        if (Count == 0) return "no data";
        string suffix = unit is null ? string.Empty : $" {unit}";
        return
            $"min: {Number(Min)}{suffix}, " +
            $"avg: {Number(Average)}{suffix}, " +
            $"max: {Number(Max)}{suffix} " +
            $"(n={Count})";
    }

    // Mesh dimensions can legitimately be far below 0.01. Fixed two-decimal
    // formatting hid useful values as 0.00 and made healthy refinement look
    // degenerate, so metrics retain six significant digits and use scientific
    // notation automatically when the magnitude requires it.
    static string Number(double value)
        => value.ToString("G6", CultureInfo.InvariantCulture);
}
