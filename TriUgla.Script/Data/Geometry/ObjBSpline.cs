namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjBSpline(
    CurveId id,
    IReadOnlyList<PointId> pointIds)
    : ObjCurve(id, pointIds)
    {
        public override DataKind Kind => DataKind.BSpline;

        public override string ToString()
            => $"BSpline({Id}) = {{{string.Join(", ", PointIds)}}}";
    }
}
