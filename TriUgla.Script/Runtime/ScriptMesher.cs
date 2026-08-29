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
                SurfaceMesh result = CompleteSurface(surface.Tag, prepared.Mesher, inserted);
                faces.AddRange(result.Faces);
                steinerNodes += result.SteinerNodes;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Plane Surface({surface.Tag}) could not be meshed: {exception.Message}", exception);
            }
        }
        return new ScriptMeshResult(faces, steinerNodes);
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
                SurfaceMesh result = CompleteSurface(surface.Tag, prepared.Mesher, inserted);
                faces.AddRange(result.Faces);
                steinerNodes += result.SteinerNodes;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Plane Surface({surface.Tag}) could not be meshed: {exception.Message}", exception);
            }
        }
        return new ScriptMeshResult(faces, steinerNodes);
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

        double z = positions[0].Z;
        if (positions.Any(position => Math.Abs(position.Z - z) > 1e-9))
        {
            throw new InvalidOperationException(
                "all points must lie in one XY plane; non-constant Z coordinates were found.");
        }

        Vec2 min = new(positions.Min(position => position.X), positions.Min(position => position.Y));
        Vec2 max = new(positions.Max(position => position.X), positions.Max(position => position.Y));
        if (min == max) throw new InvalidOperationException("the surface bounds have zero size.");
        Vec2 padding = Vec2.Make(Math.Max((max - min).Length * .1, 1e-6));
        var mesher = new Mesher(min - padding, max + padding);
        foreach (Node node in mesher.SuperStructure!.Nodes) node.Data = new NodeData(z, 0d);

        var nodes = new Dictionary<Vec2, Node>();
        Node GetNode(CurvePosition position)
        {
            CurvePosition normalized = Normalize(position);
            var key = new Vec2(normalized.X, normalized.Y);
            if (nodes.TryGetValue(key, out Node? existing)) return existing;
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

    static SurfaceMesh CompleteSurface(int surfaceTag, Mesher mesher, int steinerNodes)
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
            new RefineSettings(budget, 8, 1e-4),
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
            new RefineSettings(budget, 8, 1e-4),
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

    readonly record struct PreparedSurface(Mesher Mesher, IReadOnlyList<CurvePosition> Positions);
    readonly record struct SurfaceMesh(IReadOnlyList<ScriptMeshFace> Faces, int SteinerNodes);
}

public sealed record ScriptMeshResult(IReadOnlyList<ScriptMeshFace> Faces, int SteinerNodes);
public sealed record ScriptMeshFace(
    int SurfaceTag,
    FaceKind Kind,
    bool ContainsSuperStructure,
    IReadOnlyList<ScriptMeshVertex> Vertices);
public readonly record struct ScriptMeshVertex(double X, double Y, double Z);
