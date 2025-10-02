using System.Runtime.CompilerServices;

namespace TriUglaMesher
{
    public class Node : IPoint
    {
        public Node(double x, double y)
        {
            X = x;
            Y = y;
        }

        public int Index { get; set; } = -1;
        public int Triangle { get; set; } = -1;

        public double X { get; set; }
        public double Y { get; set; }

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
