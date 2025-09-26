using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public readonly struct Circle
    {
        public readonly double x, y, radiusSqr;

        public Circle(double x, double y, double radiusSqr)
        {
            this.x = x;
            this.y = y;
            this.radiusSqr = radiusSqr;
        }

        public Circle(double x1, double y1, double x2, double y2)
        {
            this.x = (x1 + x2) / 2.0;
            this.y = (y1 + y2) / 2.0;

            double dx = x2 - x1;
            double dy = y2 - y1;
            this.radiusSqr = (dx * dx + dy * dy) * 0.25;
        }

        public Circle(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            // https://stackoverflow.com/questions/62488827/solving-equation-to-find-center-point-of-circle-from-3-points
            var x12 = x1 - x2;
            var y12 = y1 - y2;

            var x13 = x1 - x3;
            var y13 = y1 - y3;

            var x31 = x3 - x1;
            var y31 = y3 - y1;

            var x21 = x2 - x1;
            var y21 = y2 - y1;

            var sx13 = x1 * x1 - x3 * x3;
            var sy13 = y1 * y1 - y3 * y3;
            var sx21 = x2 * x2 - x1 * x1;
            var sy21 = y2 * y2 - y1 * y1;

            var f = (sx13 * x12 + sy13 * x12 + sx21 * x13 + sy21 * x13) / (2 * (y31 * x12 - y21 * x13));
            var g = (sx13 * y12 + sy13 * y12 + sx21 * y13 + sy21 * y13) / (2 * (x31 * y12 - x21 * y13));
            var c = -(x1 * x1) - y1 * y1 - 2 * g * x1 - 2 * f * y1;

            this.x = -g;
            this.y = -f;
            this.radiusSqr = x * x + y * y - c;
        }

        public Circle(Node p0, Node p1, Node p2)
            : this(p0.X, p0.Y, p1.X, p1.Y, p2.X, p2.Y)
        {

        }

        public bool Contains(double x, double y)
        {
            double dx = this.x - x;
            double dy = this.y - y;
            return dx * dx + dy * dy < radiusSqr;
        }
    }
}
