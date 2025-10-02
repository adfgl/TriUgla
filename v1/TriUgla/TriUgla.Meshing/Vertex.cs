using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public class Vertex : IPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Seed { get; set; }
    }
}
