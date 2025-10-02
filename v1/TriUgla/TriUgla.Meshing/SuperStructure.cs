using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public static class SuperStructure
    {
        public static Mesh AddTriangle(Mesh mesh, Rectangle bounds, double scale)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;

            Node a = new Node(midx - size, midy - size, -1)
            {
                Index = 0,
                Triangle = 0
            };

            Node b = new Node(midx + size, midy - size, -1)
            {
                Index = 1,
                Triangle = 0
            };

            Node c = new Node(midx, midy + size, -1)
            {
                Index = 2,
                Triangle = 0
            };

            mesh.Nodes.Add(a);
            mesh.Nodes.Add(b);
            mesh.Nodes.Add(c);

            mesh.Circles.Add(new Circle(a, b, c));
            mesh.Triangles.Add(new Triangle(0,
                0, 1, 2,
                -1, -1, -1,
                -1, -1, -1));

            return mesh;
        }
    }
}
