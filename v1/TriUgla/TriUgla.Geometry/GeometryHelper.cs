using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TriUgla.Geometry
{
    public static class GeometryHelper
    {
        public static double LengthSquared(IPoint a, IPoint b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static double Length(IPoint a, IPoint b)
        {
            return Math.Sqrt(LengthSquared(a, b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(IPoint a, IPoint b, IPoint c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreCollinear(IPoint a, IPoint b, IPoint c, double eps)
        {
            return Math.Abs(Cross(a, b, c)) <= eps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Center(IPoint a, IPoint b, IPoint c, out double x, out double y)
        {
            x = (a.X + b.X + c.X) / 3.0;
            y = (a.Y + b.Y + c.Y) / 3.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(IPoint a, IPoint b, IPoint c)
        {
            return Cross(a, b, c) * 0.5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConvex(IPoint a, IPoint b, IPoint c, IPoint d)
        {
            return
                Cross(a, b, c) > 0 &&
                Cross(b, c, d) > 0 &&
                Cross(c, d, a) > 0 &&
                Cross(d, a, b) > 0;
        }

        public static bool PointOnSegment(IPoint a, IPoint b, double x, double y, double tolerance)
        {
            if (x < Math.Min(a.X, b.X) - tolerance || x > Math.Max(a.X, b.X) + tolerance ||
                y < Math.Min(a.Y, b.Y) - tolerance || y > Math.Max(a.Y, b.Y) + tolerance)
                return false;

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            double dxp = x - a.X;
            double dyp = y - a.Y;

            double cross = dx * dyp - dy * dxp;
            if (Math.Abs(cross) > tolerance)
                return false;

            double dot = dx * dx + dy * dy;
            if (dot < tolerance)
            {
                double ddx = a.X - x;
                double ddy = a.Y - y;
                return ddx * ddx + ddy * ddy <= tolerance;
            }

            double t = (dxp * dx + dyp * dy) / dot;
            return t >= -tolerance && t <= 1 + tolerance;
        }

        public static bool Intersect(IPoint p1, IPoint p2, IPoint q1, IPoint q2, out double x, out double y)
        {
            // P(u) = p1 + u * (p2 - p1)
            // Q(v) = q1 + v * (q2 - q1)

            // goal to vind such 'u' and 'v' so:
            // p1 + u * (p2 - p1) = q1 + v * (q2 - q1)
            // which is:
            // u * (p2x - p1x) - v * (q2x - q1x) = q1x - p1x
            // u * (p2y - p1y) - v * (q2y - q1y) = q1y - p1y

            // | p2x - p1x  -(q2x - q1x) | *  | u | =  | q1x - p1x |
            // | p2y - p1y  -(q2y - q1y) |    | v |    | q1y - p1y |

            // | a  b | * | u | = | e |
            // | c  d |   | v |   | f |

            x = y = Double.NaN;

            double a = p2.X - p1.X, b = q1.X - q2.X;
            double c = p2.Y - p1.Y, d = q1.Y - q2.Y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return false;
            }

            double e = q1.X - p1.X, f = q1.Y - p1.Y;
            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return false;
            }

            x = p1.X + u * a;
            y = p1.Y + u * c;
            return true;
        }
    }
}
