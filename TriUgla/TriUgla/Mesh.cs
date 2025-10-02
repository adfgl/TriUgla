namespace TriUgla
{
    public class Mesh
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;
        readonly int[] m_tris = new int[4];
        int m_count = 0;

        Rectangle _bounds;
        List<Node> _nodes;
        List<Circle> _circles;
        List<Triangle> _triangles;

        public Rectangle Bounds { get; set; }
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Circle> Circles { get; set; } = new List<Circle>();
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();

        public Node Add(double x, double y)
        {
            Node node = new Node(x, y)
            {
                Index = Nodes.Count
            };
            Nodes.Add(node);
            return node;
        }

        public void SetAdjacent(int parent, int parentStart, int parentEnd, int child)
        {
            if (parent == -1) return;
            Triangle t = _triangles[parent].Orient(parentStart, parentEnd);
            t.adj0 = child;
            _triangles[parent] = t;
        }

        public void SetConstraint(int triangle, int edge, int type)
        {
            Triangle t = _triangles[triangle].Orient(edge);
            t.con0 = type;

            int adjIndex = t.adj0;
            if (adjIndex != -1)
            {
                Triangle adj = _triangles[adjIndex].Orient(t.vtx1, t.vtx0);
                adj.con0 = type;
                _triangles[adjIndex] = adj;
            }
            _triangles[t.index] = t;
        }

        public Triangle EntranceTriangle(Node start, Node end, double eps)
        {
            Circler walker = new Circler(_triangles, start);
            do
            {
                Triangle t = _triangles[walker.Current];
                t = t.Orient(t.IndexOf(start.Index));

                if (Node.Cross(_nodes[t.vtx0], _nodes[t.vtx1], end) < -eps ||
                    Node.Cross(_nodes[t.vtx2], _nodes[t.vtx0], end) < -eps) continue;

                return t;

            } while (walker.Next());

            throw new Exception("Could not find entrance triangle.");
        }

        public (int t, int e) FindEdge(Node start, Node end, bool invariant)
        {
            Circler circler = new Circler(_triangles, start);

            int ai = start.Index;
            int bi = end.Index;
            do
            {
                int index = circler.Current;
                Triangle t = _triangles[index];

                int edge = t.IndexOf(ai, bi);
                if (edge == -1 && invariant)
                {
                    edge = t.IndexOf(bi, ai);
                }

                if (edge != -1)
                {
                    return (index, edge);
                }
            }
            while (circler.Next());

            return (-1, -1);
        }

        public (int t, int e, int v) FindContaining(double x, double y, double eps, List<int>? path = null, int searchStart = -1)
        {
            if (_triangles.Count == 0 || !_bounds.Contains(x, y))
            {
                return (-1, -1, -1);
            }

            int current = searchStart == -1 ? _triangles.Count - 1 : searchStart;
            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            Node vertex = new Node(x, y);
            while (trianglesChecked++ < maxSteps)
            {
                if (path is not null)
                {
                    path.Add(current);
                }

                Triangle t = _triangles[current];
                Node v0 = _nodes[t.vtx0];
                Node v1 = _nodes[t.vtx1];
                Node v2 = _nodes[t.vtx2];

                double cross01 = Node.Cross(v0, v1, vertex);
                double cross12 = Node.Cross(v1, v2, vertex);
                double cross20 = Node.Cross(v2, v0, vertex);

                int bestExitTriangle = t.adj0;
                int worstEdge = 0;
                double worstCross = cross01;

                if (cross12 < worstCross)
                {
                    worstCross = cross12;
                    bestExitTriangle = t.adj1;
                    worstEdge = 1;
                }

                if (cross20 < worstCross)
                {
                    worstCross = cross20;
                    bestExitTriangle = t.adj2;
                    worstEdge = 2;
                }

                if (Math.Abs(worstCross) <= eps)
                {
                    (int si, int ei) = t.Edge(worstEdge);

                    Node start = _nodes[si];
                    if (Node.CloseOrEqual(start, vertex, eps))
                    {
                        return (current, -1, si);
                    }

                    Node end = _nodes[ei];
                    if (Node.CloseOrEqual(end, vertex, eps))
                    {
                        return (current, -1, ei);
                    }

                    if (Rectangle.Build(start.X, start.Y, end.X, end.Y).Contains(x, y))
                    {
                        return (current, worstEdge, -1);
                    }
                }

                if (worstCross > 0)
                {
                    return (current, -1, -1);
                }
                current = bestExitTriangle;
            }
            return (-1, -1, -1);
        }


        public bool CanFlip(int triangle, int edge, out bool should)
        {
            should = false;
            Triangle t0 = _triangles[triangle].Orient(edge);
            if (t0.adj0 == -1 || t0.con0 != -1) return false;

            Node v0 = _nodes[t0.vtx0];
            Node v1 = _nodes[t0.vtx1];
            Node v2 = _nodes[t0.vtx2];

            Triangle t1 = _triangles[t0.adj0].Orient(v1.Index, v0.Index);
            Node v3 = _nodes[t1.vtx2];

            should = _circles[triangle].Contains(v3.X, v3.Y);
            return Node.IsConvex(v1, v2, v0, v3);
        }

        /// <summary>
        /// Flips the given edge of <paramref name="triangleIndex"/> (at <paramref name="edgeIndex"/>),
        /// replacing the shared diagonal with the opposite one. Updates connectivity and circles in-place.
        /// </summary>
        /// <param name="triangleIndex">Triangle index in <see cref="Mesh.Triangles"/>.</param>
        /// <param name="edgeIndex">Local edge (0..2) of the triangle to flip.</param>
        /// <param name="forceFlip">Flip even if the edge is constrained.</param>
        /// <returns>2 if flipped; 0 if not (boundary or constrained without forcing).</returns>
        public int Flip(int triangleIndex, int edgeIndex, bool forceFlip)
        {
            Triangle old0 = _triangles[triangleIndex].Orient(edgeIndex);
            int constraint = old0.con0;

            if (old0.adj0 == -1 || (constraint != -1 && !forceFlip))
            {
                return 0;
            }

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;

            Triangle old1 = _triangles[old0.adj0].Orient(i1, i0);
            int i3 = old1.vtx2;

            int t0 = old0.index;
            int t1 = old1.index;

            Node v0 = _nodes[i0];
            Node v1 = _nodes[i1];
            Node v2 = _nodes[i2];
            Node v3 = _nodes[i3];

            // the first diagonal MUST be opposite to v2 to avoid degeneracy
            // 1) 0-3-2
            // 2) 3-1-2

            _circles[t0] = new Circle(v0, v3, v2);
            _circles[t1] = new Circle(v3, v1, v2);

            _triangles[t0] = new Triangle(t0,
                i0, i3, i2,
                old1.adj1, t1, old0.adj2,
                old1.con1, constraint, old0.con2);

            _triangles[t1] = new Triangle(t1,
                i3, i1, i2,
                old1.adj2, old0.adj1, t0,
                old1.con2, old0.con1, constraint);

            SetAdjacent(old0.adj1, i2, i1, t1);
            SetAdjacent(old1.adj1, i3, i0, t0);

            v0.Triangle = v2.Triangle = v3.Triangle = t0;
            v1.Triangle = t1;

            m_tris[0] = t0;
            m_tris[1] = t1;
            return 2;
        }
    }
}
