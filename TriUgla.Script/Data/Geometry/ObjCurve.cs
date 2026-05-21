namespace TriUgla.Script.Data.Geometry
{
    public abstract class ObjCurve(
        CurveId id,
        IReadOnlyList<PointId> pointIds)
        : ObjEntity<CurveId>(id)
    {
        public IReadOnlyList<PointId> PointIds { get; } = pointIds;
    }
}
