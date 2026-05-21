namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjPoint(
        PointId id,
        double x,
        double y,
        double z,
        double meshSize)
        : ObjEntity<PointId>(id)
    {
        public double X { get; } = x;
        public double Y { get; } = y;
        public double Z { get; } = z;

        public double MeshSize { get; } = meshSize;

        public override DataKind Kind => DataKind.Point;

        public override string ToString()
            => $"Point({Id}) = {{{X}, {Y}, {Z}, {MeshSize}}}";
    }
}
