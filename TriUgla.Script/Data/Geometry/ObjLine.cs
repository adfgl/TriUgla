namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjLine(
        CurveId id,
        PointId start,
        PointId end)
        : ObjCurve(id, [start, end])
    {
        public PointId Start => PointIds[0];
        public PointId End => PointIds[1];

        public override DataKind Kind => DataKind.Line;

        public override string ToString()
            => $"Line({Id}) = {{{Start}, {End}}}";
    }
}
