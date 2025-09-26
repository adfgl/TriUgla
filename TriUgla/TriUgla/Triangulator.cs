using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                Index = 1,
                Triangle = 0
            };

            Node c = new Node(midx, midy + size)
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

        public Node? Insert(double x, double y, double eps)
        {
            (int t, int e, int v) = Finder.FindContaining(_mesh, x, y, eps);
            if (t == -1) return null;
            if (v != -1) return _mesh.Nodes[v];
            return Insert(new int[4], x, y, t, e);
        }

        public void Insert(int type, double x0, double y0, double x1, double y1, double eps, bool alwaysSplit = false)
        {
            Node? start = Insert(x0, y0, eps);
            Node? end = Insert(x1, y1, eps);
            if (type == -1 || start == null || end == null) return;

            int[] created = new int[4];

            Queue<(Node, Node)> queue = new Queue<(Node, Node)>();
            queue.Enqueue((start, end));
            while (queue.Count > 0)
            {
                var (n0, n1) = queue.Dequeue();
                if (Node.CloseOrEqual(n0, n1, 1e-4))
                {
                    continue;
                }

                (int t, int e) = Finder.FindEdge(_mesh, n0, n1, true);
                if (e != -1)
                {
                    _mesh.SetConstraint(t, e, type);
                    continue;
                }

                Triangle triangle = Finder.EntranceTriangle(_mesh, n0, n1, eps);
                t = triangle.index;
                _mesh.Triangles[t] = triangle;

                Node next = _mesh.Nodes[triangle.vtx1];
                if (Node.AreParallel(start, end, n0, next, eps))
                {
                    _mesh.SetConstraint(t, 0, type);

                    queue.Enqueue((next, n1));
                    continue;
                }

                Node prev = _mesh.Nodes[triangle.vtx2];
                if (Node.AreParallel(start, end, n0, prev, eps))
                {
                    _mesh.SetConstraint(t, 2, type);

                    queue.Enqueue((prev, n1));
                    continue;
                }

                Node? inter = Node.Intersect(start, end, next, prev);
                if (inter == null)
                {
                    throw new Exception("Expected intersection");
                }

                Triangle adjacent = _mesh.Triangles[triangle.adj1].Orient(prev.Index, next.Index);
                Node opps = _mesh.Nodes[adjacent.vtx2];

                int count;
                if (alwaysSplit || !Legalizer.CanFlip(_mesh, t, 1, out _))
                {
                    Node inserted = _mesh.Add(inter.X, inter.Y);
                    count = Splitter.Split(created, _mesh, t, 1, inserted.Index);

                    (t, e) = Finder.FindEdge(_mesh, start, inserted, true);
                    _mesh.SetConstraint(t, e, type);

                    (t, e) = Finder.FindEdge(_mesh, inserted, opps, true);
                    _mesh.SetConstraint(t, e, type);
                }
                else
                {
                    _mesh.SetConstraint(t, 1, type);
                    count = Legalizer.Flip(created, _mesh, t, 1, true);
                }

                queue.Enqueue((opps, end));
                Legalizer.Legalize(_mesh, created, count);
            }
        }

        Node Insert(int[] created, double x, double y, int triangle, int edge, Stack<int>? affected = null)
        {
            Node node = _mesh.Add(x, y);
            int count;
            if (edge == -1)
            {
                count = Splitter.Split(created, _mesh, triangle, node.Index);
            }
            else
            {
                count = Splitter.Split(created, _mesh, triangle, edge, node.Index);
            }
            int legalized = Legalizer.Legalize(_mesh, created, count, affected);
            return node;
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
