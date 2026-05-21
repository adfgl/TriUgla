namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjCircle(
        CurveId id,
        PointId start,
        PointId center,
        PointId end)
        : ObjCurve(id, [start, center, end])
    {
        public PointId Start => PointIds[0];
        public PointId Center => PointIds[1];
        public PointId End => PointIds[2];

        public override DataKind Kind => DataKind.Circle;

        public override string ToString()
            => $"Circle({Id}) = {{{Start}, {Center}, {End}}}";
    }
}
