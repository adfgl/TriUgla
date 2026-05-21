namespace TriUgla.Script.Data
{
    public enum DataKind : byte
    {
        Undefined,

        Integer,
        Double,
        Boolean,
        String,
        List,
        Range,

        Point,

        Line,
        LineLoop,
        Circle,
        Ellipse,
        Spline,
        BSpline,
        Bezier,

        CurveLoop,
        PlaneSurface,
        SurfaceLoop,
        Volume,

        PhysicalPoint,
        PhysicalCurve,
        PhysicalSurface,
        PhysicalVolume,

        TransfiniteCurve,
        TransfiniteSurface,
        RecombineSurface,

        EmbedConstraint,

        MeshCommand,
        MeshOption
    }
}
