using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public class Node : IPoint
    {
        public Node(double x, double y, double seed)
        {
            X = x;
            Y = y;
            Seed = seed;
        }

        public int Index { get; set; } = -1;
        public int Triangle { get; set; } = -1;

        public double X { get; set; }
        public double Y { get; set; }
        public double Seed { get; set; } 

        public static double Interpolate(Node a, Node b, double x, double y)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double len2 = dx * dx + dy * dy;
            if (len2 <= double.Epsilon)
            {
                return 0.5 * (a.Seed + b.Seed);
            }

            double t = ((x - a.X) * dx + (y - a.Y) * dy) / len2;
            if (t < 0) t = 0; else if (t > 1) t = 1;
            return a.Seed + t * (b.Seed - a.Seed);
        }

        public static bool CloseOrEqual(Node a, Node b, double eps)
        {
            if (a.Index == b.Index && a.Index != -1)
            {
                return true;
            }
            return GeometryHelper.LengthSquared(a, b) <= eps;
        }
    }
}
