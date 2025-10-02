using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public class Vertex : IPoint
    {
        public int Key { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Seed { get; set; }
    }
}
