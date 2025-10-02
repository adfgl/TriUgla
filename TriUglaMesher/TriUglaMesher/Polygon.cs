namespace TriUglaMesher
{
    public class Polygon
    {
        readonly List<IPoint> _points;
        readonly Rectangle _bounds;

        public Polygon(IEnumerable<IPoint> points)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;

            _points = new List<IPoint>();
            foreach (IPoint point in points)
            {
                double x = point.X;
                double y = point.Y;

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                _points.Add(point);
            }

            IPoint first = points.First();
            IPoint last = points.Last();
            if (!first.Equals(last))
            {
                _points.Add(first);
            }
            _bounds = new Rectangle(minX, minY, maxX, maxY);
        }


        public Rectangle Bounds => _bounds;
        public IReadOnlyList<IPoint> Points => _points;

        public bool Contains(double x, double y, double eps)
        {
            if (_bounds.Contains(x, y))
            {
                return Contains(_points, x, y, eps);
            }
            return false;
        }

        public bool Intersects(Polygon other)
        {
            if (_bounds.Intersects(other._bounds))
            {
                List<IPoint> av = _points, ab = other._points;
                int ac = av.Count, bc = ab.Count;
                for (int i = 0; i < ac - 1; i++)
                {
                    IPoint p1 = av[i];
                    IPoint p2 = av[i + 1];
                    for (int j = 0; j < bc - 1; j++)
                    {
                        IPoint q1 = ab[j];
                        IPoint q2 = ab[j + 1];
                        if (GeometryHelper.Intersect(p1, p2, q1, q2, out _, out _))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool Contains(Polygon other, double eps)
        {
            if (_bounds.Contains(other._bounds))
            {
                foreach (IPoint point in _points.Skip(1))
                {
                    if (!Contains(point.X, point.Y, eps))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool Contains(List<IPoint> closedPolygon, double x, double y, double eps)
        {
            int count = closedPolygon.Count - 1;
            bool inside = false;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                IPoint a = closedPolygon[i];
                IPoint b = closedPolygon[j];
                if (PointOnSegment(a, b, x, y, eps))
                {
                    return true;
                }

                double xi = a.X, yi = a.Y;
                double xj = b.X, yj = b.Y;

                bool crosses = (yi > y + eps) != (yj > y + eps);
                if (!crosses) continue;

                double t = (y - yi) / (yj - yi + double.Epsilon);
                double xCross = xi + t * (xj - xi);

                if (x < xCross - eps)
                    inside = !inside;
            }

            return inside;
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
    }
}
