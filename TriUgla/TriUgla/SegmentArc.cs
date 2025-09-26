using System.Runtime.CompilerServices;

namespace TriUgla
{
    public sealed class SegmentArc : Segment
    {
        public SegmentNode Center { get; }
        public double Radius { get; }
        public double StartAngle { get; }   // radians
        public double SweepAngle { get; }   // signed, CCW positive

        private readonly SegmentNode _start;
        private readonly SegmentNode _end;

        public override SegmentNode Start => _start;
        public override SegmentNode End => _end;
        public bool Clockwise => SweepAngle < 0;
        public override double Length => Math.Abs(SweepAngle) * Radius;

        private SegmentArc(SegmentNode center, double radius, double startAngle, double sweepAngle)
        {
            Center = center;
            Radius = radius;
            StartAngle = NormalizeAngle(startAngle);
            SweepAngle = sweepAngle;

            _start = AtAngle(StartAngle);
            _end = AtAngle(StartAngle + SweepAngle);
        }

        public override SegmentNode PointAt(double t)
        {
            double theta = StartAngle + SweepAngle * t;
            return AtAngle(theta);
        }

        public override IReadOnlyList<Segment> Split(int parts)
        {
            if (parts <= 0) return Array.Empty<Segment>();
            var result = new Segment[parts];
            double inv = 1.0 / parts;

            for (int i = 0; i < parts; i++)
            {
                double th0 = StartAngle + SweepAngle * (i * inv);
                double th1 = StartAngle + SweepAngle * ((i + 1) * inv);
                double subSweep = th1 - th0;

                result[i] = new SegmentArc(Center, Radius, th0, subSweep);
            }
            return result;
        }

        private SegmentNode AtAngle(double theta)
            => new SegmentNode(Center.X + Radius * Math.Cos(theta),
                               Center.Y + Radius * Math.Sin(theta));

        public static SegmentArc FromCenterAngles(SegmentNode center, double radius, double startAngle, double sweepAngle)
            => new SegmentArc(center, radius, startAngle, sweepAngle);

        public static SegmentArc FromEndpointsCenter(SegmentNode a, SegmentNode b, SegmentNode center, bool clockwise, bool shortest = true)
        {
            double ax = a.X - center.X, ay = a.Y - center.Y;
            double bx = b.X - center.X, by = b.Y - center.Y;

            double r = 0.5 * (Math.Sqrt(ax * ax + ay * ay) + Math.Sqrt(bx * bx + by * by));

            double angA = Math.Atan2(ay, ax);
            double angB = Math.Atan2(by, bx);

            double sweep = clockwise
                ? -NormalizePositive(angA - angB)
                : NormalizePositive(angB - angA);

            if (!shortest)
            {
                if (sweep > 0) sweep -= 2 * Math.PI;
                else if (sweep < 0) sweep += 2 * Math.PI;
            }
            return new SegmentArc(center, r, angA, sweep);
        }

        public static SegmentArc FromThreePoints(SegmentNode a, SegmentNode m, SegmentNode b)
        {
            CircleFrom3(a, m, b, out var center, out var r);

            double angA = Math.Atan2(a.Y - center.Y, a.X - center.X);
            double angB = Math.Atan2(b.Y - center.Y, b.X - center.X);
            double angM = Math.Atan2(m.Y - center.Y, m.X - center.X);

            double ccw = NormalizePositive(angB - angA);
            bool mOnCcw = NormalizePositive(angM - angA) <= ccw + 1e-14;
            double sweep = mOnCcw ? ccw : -(2 * Math.PI - ccw);

            return new SegmentArc(center, r, angA, sweep);
        }

        public static SegmentArc FromBulge(SegmentNode a, SegmentNode b, double bulge)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double c = Math.Sqrt(dx * dx + dy * dy);
            if (c == 0) throw new ArgumentException("Degenerate chord length.");

            double sweep = 4.0 * Math.Atan(bulge); // signed
            double alpha = Math.Abs(sweep) * 0.5;

            double r = c / (2.0 * Math.Sin(alpha));
            double d = (c * 0.5) * Cot(alpha);

            var mid = new SegmentNode((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
            var n = LeftUnitNormal(dx, dy);
            double sgn = Math.Sign(sweep);

            var center = new SegmentNode(mid.X + sgn * d * n.X, mid.Y + sgn * d * n.Y);
            double angA = Math.Atan2(a.Y - center.Y, a.X - center.X);

            return new SegmentArc(center, r, angA, sweep);
        }

        public static SegmentArc FromChordSagitta(SegmentNode a, SegmentNode b, double sagitta)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double c = Math.Sqrt(dx * dx + dy * dy);
            if (c == 0) throw new ArgumentException("Degenerate chord length.");

            double R = (c * c) / (8.0 * Math.Abs(sagitta)) + Math.Abs(sagitta) * 0.5;
            double alpha = Math.Asin((c * 0.5) / R);
            double sweep = 2.0 * alpha;
            if (sagitta < 0) sweep = -sweep;

            var mid = new SegmentNode((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
            var n = LeftUnitNormal(dx, dy);
            double d = R - Math.Abs(sagitta);
            double sgn = Math.Sign(sagitta);

            var center = new SegmentNode(mid.X + sgn * d * n.X, mid.Y + sgn * d * n.Y);
            double angA = Math.Atan2(a.Y - center.Y, a.X - center.X);

            return new SegmentArc(center, R, angA, sweep);
        }

        public static SegmentArc FromStartTangentRadius(SegmentNode start, (double X, double Y) tangent, double radius, double sweepAngle)
        {
            double tlen = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
            if (tlen == 0) throw new ArgumentException("Zero tangent.");

            double tx = tangent.X / tlen, ty = tangent.Y / tlen;
            double nx = -ty, ny = tx;
            double sgn = sweepAngle >= 0 ? 1.0 : -1.0;

            var center = new SegmentNode(start.X + sgn * radius * nx, start.Y + sgn * radius * ny);
            double ang0 = Math.Atan2(start.Y - center.Y, start.X - center.X);

            return new SegmentArc(center, radius, ang0, sweepAngle);
        }

        public static SegmentArc FromArcLength(SegmentNode center, double radius, double startAngle, double arcLength, int sign = +1)
        {
            double sweep = (arcLength / radius) * Math.Sign(sign);
            return new SegmentArc(center, radius, startAngle, sweep);
        }

        static void CircleFrom3(SegmentNode a, SegmentNode b, SegmentNode c, out SegmentNode center, out double r)
        {
            double ax = a.X, ay = a.Y, bx = b.X, by = b.Y, cx = c.X, cy = c.Y;

            double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (Math.Abs(d) < 1e-18) throw new ArgumentException("Collinear points.");

            double a2 = ax * ax + ay * ay;
            double b2 = bx * bx + by * by;
            double c2 = cx * cx + cy * cy;

            double ux = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / d;
            double uy = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d;

            center = new SegmentNode(ux, uy);
            double dx = ax - ux, dy = ay - uy;
            r = Math.Sqrt(dx * dx + dy * dy);
        }

        static (double X, double Y) LeftUnitNormal(double dx, double dy)
        {
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len == 0 ? (0, 0) : (-dy / len, dx / len);
        }

        static double NormalizeAngle(double a)
        {
            double twoPi = 2 * Math.PI;
            a %= twoPi;
            if (a < 0) a += twoPi;
            return a;
        }

        static double NormalizePositive(double a)
        {
            double twoPi = 2 * Math.PI;
            a %= twoPi;
            if (a < 0) a += twoPi;
            return a;
        }

        static double Cot(double a) => Math.Cos(a) / Math.Sin(a);
    }
}
