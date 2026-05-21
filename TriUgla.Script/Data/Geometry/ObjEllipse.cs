namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjEllipse(
        CurveId id,
        PointId start,
        PointId center,
        PointId major,
        PointId end)
        : ObjCurve(id, [start, center, major, end])
    {
        public override DataKind Kind => DataKind.Ellipse;

        public override string ToString()
            => $"Ellipse({Id}) = {{{string.Join(", ", PointIds)}}}";
    }
}
