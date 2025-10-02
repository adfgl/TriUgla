using System.Runtime.CompilerServices;

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
                if (Node.CloseOrEqual(n0, n1, eps))
                {
                    continue;
                }

                Triangle triangle = Finder.EntranceTriangle(_mesh, n0, n1, eps);
                int t = triangle.index;
                _mesh.Triangles[t] = triangle;

                int e = triangle.IndexOfInvariant(n0.Index, n1.Index);
                if (e != -1)
                {
                    _mesh.SetConstraint(t, e, type);
                    continue;
                }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public static bool ContainsSuper(Triangle t)
        {
            return t.vtx0 < 3 || t.vtx1 < 3 || t.vtx2 < 3;
        }

        bool Bad(Triangle t, Quality qt)
        {
            Node a = _mesh.Nodes[t.vtx0];
            Node b = _mesh.Nodes[t.vtx1];
            Node c = _mesh.Nodes[t.vtx2];

            double area = Node.Cross(a, b, c) * 0.5;
            return area > qt.MaxArea;
        }

        public Triangulator Refine(Quality quality, double eps)
        {
            if (!quality.ShouldRefine()) return this;

            if (_state != ETriangulatorState.Triangulated || _state == ETriangulatorState.Finalized)
            {
                return this;
            }

            HashSet<RefinedEdge> seen = new HashSet<RefinedEdge>();
            Queue<int> triangleQueue = new Queue<int>();
            Queue<RefinedEdge> segmentQueue = new Queue<RefinedEdge>();
            int[] created = new int[4];

            QuadTree<Node> qt = new QuadTree<Node>(_mesh.Bounds, 64);
            foreach (Node item in _mesh.Nodes.Skip(3)) qt.Add(item);

            foreach (Triangle t in _mesh.Triangles)
            {
                if (ContainsSuper(t)) continue;

                if (Bad(t, quality))
                {
                    triangleQueue.Enqueue(t.index);
                }

                addSegment(t.con0, t.vtx0, t.vtx1);
                addSegment(t.con1, t.vtx1, t.vtx2);
                addSegment(t.con2, t.vtx2, t.vtx0);
            }

            Stack<int> affected = new Stack<int>();
            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                while (affected.Count > 0)
                    triangleQueue.Enqueue(affected.Pop());

                if (segmentQueue.Count > 0)
                {
                    RefinedEdge constraint = segmentQueue.Dequeue();
                    (int t, int e) = Finder.FindEdge(_mesh, constraint.start, constraint.end, true);
                    if (e == -1) continue;

                    double x = constraint.circle.x;
                    double y = constraint.circle.y;

                    Node inserted = Insert(created, x, y, t, e, affected);
                    qt.Add(inserted);

                    seen.Remove(constraint);
                    foreach (RefinedEdge edge in constraint.Split(inserted, eps))
                    {
                        if (seen.Add(edge) && edge.Enchrouched(qt, eps) && edge.VisibleFromInterior(seen, x, y))
                        {
                            segmentQueue.Enqueue(edge);
                        }
                    }
                    continue;
                }

                if (triangleQueue.Count > 0)
                {
                    int ti = triangleQueue.Dequeue();
                    Triangle t = _mesh.Triangles[ti];
                    if (!Bad(t, quality)) continue;

                    Circle c = _mesh.Circles[ti];
                    double x = c.x;
                    double y = c.y;
                    if (!_mesh.Bounds.Contains(x, y))
                    {
                        continue;
                    }

                    bool encroaches = false;
                    foreach (RefinedEdge seg in seen)
                    {
                        if (seg.circle.Contains(x, y) && seg.VisibleFromInterior(seen, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    Node? inserted = Insert(x, y, eps);
                    if (inserted != null)
                    {
                        qt.Add(inserted);
                    }
                }
            }

            _state = ETriangulatorState.Refined;
            return this;

            void addSegment(int constraint, int a, int b)
            {
                if (constraint == -1) return;

                RefinedEdge edge = new RefinedEdge(constraint, _mesh.Nodes[a], _mesh.Nodes[b]);
                if (seen.Add(edge) && edge.Enchrouched(qt, eps))
                {
                    segmentQueue.Enqueue(edge);
                }
            }
        }

        public Triangulator Finalize()
        {
            if (_state != ETriangulatorState.Triangulated || _state == ETriangulatorState.Finalized)
            {
                return this;
            }

            _state = ETriangulatorState.Finalized;
            return this;
        }

        readonly struct RefinedEdge : IEquatable<RefinedEdge>
        {
            public readonly int type;
            public readonly Node start, end;
            public readonly Circle circle;

            public RefinedEdge(int type, Node start, Node end)
            {
                this.type = type;
                this.circle = new Circle(start.X, start.Y, end.X, end.Y);

                if (start.Index < end.Index)
                {
                    this.start = start;
                    this.end = end;
                }
                else
                {
                    this.start = end;
                    this.end = start;
                }
            }

            public bool Contains(Node node, double eps)
            {
                return Node.CloseOrEqual(start, node, eps) || Node.CloseOrEqual(end, node, eps);
            }

            public RefinedEdge[] Split(Node node, double eps)
            {
                if (Contains(node, eps))
                {
                    return [this];
                }
                return [new RefinedEdge(type, start, node), new RefinedEdge(type, node, end)];
            }

            public override int GetHashCode() => HashCode.Combine(start.Index, end.Index);

            public bool Equals(RefinedEdge other)
            {
                return start.Index == other.start.Index && end.Index == other.end.Index;
            }

            public override bool Equals(object? obj)
            {
                return obj is Edge other && Equals(other);
            }

            public bool VisibleFromInterior(IEnumerable<RefinedEdge> segments, double x, double y)
            {
                Node center = new Node(circle.x, circle.y);
                Node pt = new Node(x, y);
                foreach (var s in segments)
                {
                    if (this.Equals(s))
                        continue;

                    if (Node.Intersect(center, pt, s.start, s.end) is not null)
                    {
                        return false;
                    }
                }
                return true;
            }

            public bool Enchrouched(List<Node> nodes, double eps)
            {
                foreach (Node item in nodes)
                {
                    if (circle.Contains(item.X, item.Y) && !Contains(item, eps))
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool Enchrouched(QuadTree<Node> qt, double eps)
            {
                List<Node> points = qt.Query(new Rectangle(circle));
                return Enchrouched(points, eps);
            }
        }
    }
}
