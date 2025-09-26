using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public enum ETriangulatorState
    {
        None,
        Triangulated,
        Refined,
        Finalized,
    }

    public class Triangulator
    {
        ETriangulatorState _state = ETriangulatorState.None;
        Mesh _mesh;

        public Triangulator(Rectangle rectangle)
        {
            _mesh = new Mesh()
            {
                Bounds = rectangle,
            };

        }

        public ETriangulatorState State => _state;
        public Mesh Mesh => _mesh;

        static Mesh AddSuperStructure(Mesh mesh, double scale)
        {
            Rectangle bounds = mesh.Bounds;
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;


            Node a = new Node(midx - size, midy - size)
            {
                Index = 0,
                Triangle = 0
            };

            Node b = new Node(midx + size, midy - size)
            {
                Index = 0,
                Triangle = 0
            };

            Node c = new Node(midx, midy + size)
            {
                Index = 0,
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

        public void Insert(double x, double y)
        {

        }

        public void Insert(double x0, double y0, double x1, double y1)
        {

        }

        public Triangulator Reset(Rectangle? rectangle = null)
        {
            _mesh = new Mesh()
            {
                Bounds = rectangle ?? _mesh.Bounds
            };
            _state = ETriangulatorState.None;
            return this;
        }

        public Triangulator Triangulate()
        {
            if (_state != ETriangulatorState.None)
            {
                return this;
            }

            _mesh = AddSuperStructure(_mesh, 2);

            int[] created = new int[4];

            _state = ETriangulatorState.Triangulated;
            return this;
        }

        public Triangulator Refine()
        {
            if (_state != ETriangulatorState.Triangulated && _state != ETriangulatorState.Refined)
            {
                return this;
            }

            _state = ETriangulatorState.Refined;
            return this;
        }

        public Triangulator Finalize()
        {
            if (_state != ETriangulatorState.Triangulated && _state != ETriangulatorState.Refined)
            {
                return this;
            }

            _state = ETriangulatorState.Finalized;
            return this;
        }
    }
}
