using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public class Polygon<T> where T : IPoint
    {
        readonly List<T> _points;
        readonly Rectangle _bounds;

        public Polygon(IEnumerable<T> points)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;

            _points = new List<T>();
            foreach (T point in points)
            {
                double x = point.X;
                double y = point.Y;

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                _points.Add(point);
            }

            T first = points.First();
            T last = points.Last();
            if (!first.Equals(last))
            {
                _points.Add(first);
            }
            _bounds = new Rectangle(minX, minY, maxX, maxY);
        }

        public Rectangle Bounds => _bounds;
        public IReadOnlyList<T> Points => _points;

        public IEnumerable<(T A, T B)> GetEdges()
        {
            int count = _points.Count;
            for (int i = 0; i < count - 1; i++)
            {
                yield return (_points[i], _points[i + 1]);
            }
        }

        public bool Contains(double x, double y, double eps)
        {
            if (_bounds.Contains(x, y))
            {
                return Contains(_points, x, y, eps);
            }
            return false;
        }

        public bool Intersects(Polygon<T> other)
        {
            if (_bounds.Intersects(other._bounds))
            {
                List<T> av = _points, ab = other._points;
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

        public bool Contains(Polygon<T> other, double eps)
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

        public static bool Contains(List<T> closedPolygon, double x, double y, double eps)
        {
            int count = closedPolygon.Count - 1;
            bool inside = false;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                T a = closedPolygon[i];
                T b = closedPolygon[j];
                if (GeometryHelper.PointOnSegment(a, b, x, y, eps))
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
    }
}
