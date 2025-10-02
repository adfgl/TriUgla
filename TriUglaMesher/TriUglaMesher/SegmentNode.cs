using System.Runtime.CompilerServices;

namespace TriUglaMesher
{
    public class SegmentNode : IPoint
    {
        public SegmentNode(double x, double y)
        {
            X = x; Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(SegmentNode a, SegmentNode b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString()
        {
            return $"{X} {Y}";
        }
    }
}
