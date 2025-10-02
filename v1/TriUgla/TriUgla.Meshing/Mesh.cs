using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Geometry;

namespace TriUgla.Meshing
{
    public class Mesh
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        readonly int[] m_newTris = new int[4];

        public List<Node> Nodes {  get; set; } = new List<Node>();
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();
        public List<Circle> Circles { get; set; } = new List<Circle>();

        Node Add(double x, double y, double seed)
        {
            Node node = new Node(x, y, seed)
            {
                Index = Nodes.Count
            };
            Nodes.Add(node);
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool AreCollinear(Node a, Node b, Node c, double eps) => Math.Abs(GeometryHelper.Cross(a, b, c)) <= eps;

        public List<(Node, Node)> Insert(int type, int start, int end, double eps, bool alwaysSplit = false)
        {
            List<(Node, Node)> edges = new List<(Node, Node)>();
            List<Node> nodes = Nodes;
            if (start < 0 || start > nodes.Count ||
                end < 0 || end > nodes.Count)
            {
                return edges;
            }

            Queue<(Node, Node)> queue = new Queue<(Node, Node)>();
            queue.Enqueue((nodes[start], nodes[end]));

            List<int> toLegalize = new List<int>();
            while (queue.Count > 0)
            {
                var (startNode, endNode) = queue.Dequeue();
                if (Node.CloseOrEqual(startNode, endNode, eps)) continue;

                Triangle t = OrientedEntranceTriangle(startNode, endNode, eps);
                int ti = t.index;

                if (SetConstraint(startNode, endNode, type))
                {
                    edges.Add((startNode, endNode));
                    continue;
                }

                Node next = nodes[t.vtx1];
                if (AreCollinear(startNode, endNode, next, eps))
                {
                    queue.Enqueue((startNode, next));
                    queue.Enqueue((next, endNode));
                    continue;
                }

                Node prev = nodes[t.vtx2];
                if (AreCollinear(startNode, endNode, prev, eps))
                {
                    queue.Enqueue((startNode, prev));
                    queue.Enqueue((prev, endNode));
                    continue;
                }

                if (!GeometryHelper.Intersect(startNode, endNode, next, prev, out double x, out double y))
                {
                    throw new Exception("Expected intersection");
                }

                Triangle adjacent = Triangles[t.adj1].Orient(prev.Index, next.Index);
                Node oppositeNode = nodes[adjacent.vtx2];

                int count;
                if (alwaysSplit || !CanFlip(ti, 1, out _))
                {
                    double seed1 = Node.Interpolate(startNode, endNode, x, y);
                    double seed2 = Node.Interpolate(next, prev, x, y);
                    double seed = (seed1 + seed2) * 0.5;

                    Node inserted = Add(x, y, seed);

                    count = SplitEdge(ti, 1, inserted.Index);

                    queue.Enqueue((startNode, inserted));
                    queue.Enqueue((inserted, oppositeNode));
                    queue.Enqueue((oppositeNode, endNode));
                }
                else
                {
                    count = Flip(ti, 1, false);

                    queue.Enqueue((startNode, oppositeNode));
                    queue.Enqueue((oppositeNode, endNode));
                }

                toLegalize.AddRange(m_newTris.Take(count));
            }

            Legalize(toLegalize);
            return edges;
        }

        public Node? Insert(double x, double y, double seed, double eps)
        {
            (int t, int e, int v) = FindContaining(x, y, eps);
            if (t == -1) return null;
            if (v != -1) return Nodes[v];
            return Insert(x, y, seed, t, e);
        }

        Node Insert(double x, double y, double seed, int triangle, int edge, Stack<int>? affected = null)
        {
            Node node = Add(x, y, seed);
            int count = Split(triangle, edge, node.Index);
            int legalized = Legalize(m_newTris.Take(count), affected);
            return node;
        }

        public int Legalize(IEnumerable<int> indices, Stack<int>? affected = null)
        {
            Stack<int> stack = new Stack<int>(indices);

            int totalFlips = 0;
            List<Triangle> tris = Triangles;
            Dictionary<Edge, int> flipCount = new Dictionary<Edge, int>(64);
            while (stack.Count > 0)
            {
                int ti = stack.Pop();
                affected?.Push(ti);

                Triangle t = tris[ti];
                for (int ei = 0; ei < 3; ei++)
                {
                    var (u, v) = t.Edge(ei);
                    Edge key = new Edge(u, v);

                    if (flipCount.TryGetValue(key, out int flipsMade) && flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                    {
                        continue;
                    }

                    if (!CanFlip(ti, ei, out bool should) || !should)
                    {
                        continue;
                    }

                    totalFlips++;
                    flipCount[key] = flipsMade + 1;

                    for (int i = 0; i < Flip(ti, ei, false); i++)
                    {
                        int idx = m_newTris[i];
                        stack.Push(idx);

                        if (ti != idx && affected is not null)
                        {
                            affected.Push(idx);
                        }
                    }

                    stack.Push(ti);
                    break;
                }
            }
            return totalFlips;
        }

        /// <summary>
        /// Performs a 2–2 edge flip on <paramref name="edgeIndex"/> of <paramref name="triangleIndex"/>.
        /// Updates connectivity and circumcircles. Skips if boundary or constrained (unless <paramref name="forceFlip"/>).
        /// </summary>
        /// <param name="triangleIndex">Triangle index in <see cref="Triangles"/>.</param>
        /// <param name="edgeIndex">Local edge (0..2) to flip.</param>
        /// <param name="forceFlip">Flip even if the edge is constrained.</param>
        /// <returns>
        /// 2 if flipped, 0 otherwise. Updated triangle indices are written to <c>m_newTris[0]</c> and <c>m_newTris[1]</c>.
        /// </returns>
        public int Flip(int triangleIndex, int edgeIndex, bool forceFlip)
        {
            List<Triangle> triangles = Triangles;
            List<Node> vertices = Nodes;
            List<Circle> circles = Circles;

            Triangle old0 = triangles[triangleIndex].Orient(edgeIndex);
            int constraint = old0.con0;

            if (old0.adj0 == -1 || (constraint != -1 && !forceFlip))
            {
                return 0;
            }

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;

            Triangle old1 = triangles[old0.adj0].Orient(i1, i0);
            int i3 = old1.vtx2;

            int t0 = old0.index;
            int t1 = old1.index;

            Node v0 = vertices[i0];
            Node v1 = vertices[i1];
            Node v2 = vertices[i2];
            Node v3 = vertices[i3];

            // the first diagonal MUST be opposite to v2 to avoid degeneracy
            // 1) 0-3-2
            // 2) 3-1-2

            circles[t0] = new Circle(v0, v3, v2);
            circles[t1] = new Circle(v3, v1, v2);


            triangles[t0] = new Triangle(t0,
                i0, i3, i2,
                old1.adj1, t1, old0.adj2,
                old1.con1, constraint, old0.con2);

            triangles[t1] = new Triangle(t1,
                i3, i1, i2,
                old1.adj2, old0.adj1, t0,
                old1.con2, old0.con1, constraint);

            SetAdjacent(old0.adj1, i2, i1, t1);
            SetAdjacent(old1.adj1, i3, i0, t0);

            v0.Triangle = v2.Triangle = v3.Triangle = t0;
            v1.Triangle = t1;

            m_newTris[0] = t0;
            m_newTris[1] = t1;
            return 2;
        }

        public int Split(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            if (edgeIndex == -1)
            {
                return SplitEdge(triangleIndex, edgeIndex, vertexIndex);    
            }
            return SplitTriangle(triangleIndex, vertexIndex);
        }

        /// <summary>
        /// Splits the triangle by inserting an interior vertex. Updates connectivity and circumcircles.
        /// </summary>
        /// <param name="triangleIndex">Triangle index in <see cref="Triangles"/>.</param>
        /// <param name="vertexIndex">Vertex index in <see cref="Nodes"/> (assumed inside).</param>
        /// <returns>
        /// 3. Resulting triangle indices are written to <c>m_newTris[0..2]</c>.
        /// </returns>
        public int SplitTriangle(int triangleIndex, int vertexIndex)
        {
            List<Triangle> triangles = Triangles;
            List<Node> vertices = Nodes;
            List<Circle> circles = Circles;

            Triangle old = triangles[triangleIndex];

            int i0 = old.vtx0;
            int i1 = old.vtx1;
            int i2 = old.vtx2;
            int i3 = vertexIndex;

            Node v0 = vertices[i0];
            Node v1 = vertices[i1];
            Node v2 = vertices[i2];
            Node v3 = vertices[i3];

            // 1) 0-1-3
            // 2) 1-2-3
            // 3) 2-0-3
            int t0 = old.index;
            int t1 = triangles.Count;
            int t2 = t1 + 1;

            circles[t0] = new Circle(v0, v1, v3);
            circles.Add(new Circle(v1, v2, v3));
            circles.Add(new Circle(v2, v0, v3));

            triangles[t0] = new Triangle(t0,
                i0, i1, i3,
                old.adj0, t1, t2,
                old.con0, -1, -1);

            triangles.Add(new Triangle(t1,
                i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1));

            triangles.Add(new Triangle(t2,
               i2, i0, i3,
               old.adj2, t0, t1,
               old.con2, -1, -1));

            SetAdjacent(old.adj1, i2, i1, t1);
            SetAdjacent(old.adj2, i0, i2, t2);

            v0.Triangle = v1.Triangle = v3.Triangle = t0;
            v2.Triangle = t1;

            m_newTris[0] = t0;
            m_newTris[1] = t1;
            m_newTris[2] = t2;
            return 3;
        }

        /// <summary>
        /// Splits using a vertex on the given local edge. Boundary → 2 triangles; interior pair → 4.
        /// Updates connectivity and circumcircles.
        /// </summary>
        /// <param name="triangleIndex">Triangle index in <see cref="Triangles"/>.</param>
        /// <param name="edgeIndex">Local edge (0..2) to split along.</param>
        /// <param name="vertexIndex">Vertex index in <see cref="Nodes"/> (on the edge).</param>
        /// <returns>
        /// 2 for boundary, 4 for interior. Resulting triangle indices are written to <c>m_newTris[0..count-1]</c>.
        /// </returns>
        public int SplitEdge(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            List<Triangle> triangles = Triangles;
            List<Node> vertices = Nodes;
            List<Circle> circles = Circles;

            Triangle old0 = triangles[triangleIndex].Orient(edgeIndex);
            int con = old0.con0;

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;

            Node v0 = vertices[i0];
            Node v1 = vertices[i1];
            Node v2 = vertices[i2];
            Node v3 = vertices[i3];

            int adj = old0.adj0;
            if (adj == -1)
            {
                int t0 = old0.index;
                int t1 = triangles.Count;

                // 1) 2-0-3
                // 2) 1-2-3
                circles[t0] = new Circle(v2, v0, v3);
                circles.Add(new Circle(v2, v1, v3));

                triangles[t0] = new Triangle(t0,
                    i0, i1, i3,
                    old0.adj2, -1, t1,
                    old0.con2, con, -1);

                triangles.Add(new Triangle(t1,
                    i2, i1, i3,
                    old0.adj1, t0, -1,
                    old0.con1, -1, con));

                SetAdjacent(old0.adj1, i2, i1, t1);

                v0.Triangle = v2.Triangle = v3.Triangle = t0;
                v1.Triangle = t1;

                m_newTris[0] = t0;
                m_newTris[1] = t1;
                return 2;
            }
            else
            {
                Triangle old1 = triangles[adj].Orient(i1, i0);

                int t0 = old0.index;
                int t1 = old1.index;
                int t2 = triangles.Count;
                int t3 = t2 + 1;

                int i4 = old1.vtx2;
                Node v4 = vertices[i4];

                // 1) 2-0-3
                // 2) 0-4-3
                // 3) 4-1-3
                // 4) 1-2-3

                circles[t0] = new Circle(v2, v0, v3);
                circles[t1] = new Circle(v0, v4, v3);
                circles.Add(new Circle(v4, v1, v3));
                circles.Add(new Circle(v1, v2, v3));

                triangles[t0] = new Triangle(t0,
                    i2, i0, i3,
                    old0.adj2, t1, t3,
                    old0.con2, con, -1);

                triangles[t1] = new Triangle(t1,
                    i0, i4, i3,
                    old1.adj1, t2, t0,
                    old1.con1, -1, con);

                triangles.Add(new Triangle(t2,
                    i4, i1, i3,
                    old0.adj2, t3, t1,
                    old0.con2, con, -1));

                triangles.Add(new Triangle(t3,
                    i1, i2, i3,
                    old0.adj1, t0, t2,
                    old0.con1, -1, con));

                SetAdjacent(old0.adj1, i2, i1, t3);
                SetAdjacent(old1.adj2, i1, i3, t2);

                v0.Triangle = v2.Triangle = t0;
                v1.Triangle = t3;
                v3.Triangle = t1;

                m_newTris[0] = t0;
                m_newTris[1] = t1;
                m_newTris[2] = t2;
                m_newTris[3] = t3;
                return 4;
            }
        }

        public Triangle OrientedEntranceTriangle(Node start, Node end, double eps)
        {
            List<Node> nodes = Nodes;
            List<Triangle> tris = Triangles;

            Circler walker = new Circler(tris, start);
            do
            {
                Triangle t = tris[walker.Current];
                t = t.Orient(t.IndexOf(start.Index));

                if (GeometryHelper.Cross(nodes[t.vtx0], nodes[t.vtx1], end) < -eps ||
                    GeometryHelper.Cross(nodes[t.vtx2], nodes[t.vtx0], end) < -eps) continue;

                tris[t.index] = t;
                return t;

            } while (walker.Next());

            throw new Exception("Could not find entrance triangle.");
        }

        public void SetAdjacent(int parent, int parentStart, int parentEnd, int child)
        {
            if (parent == -1) return;
            List<Triangle> tris = Triangles;
            Triangle t = tris[parent].Orient(parentStart, parentEnd);
            t.adj0 = child;
            tris[parent] = t;
        }

        public bool SetConstraint(Node start, Node end, int type)
        {
            Circler walker = new Circler(Triangles, start);
            do
            {
                int ti = walker.Current;
                Triangle t = Triangles[ti];
                int edge = t.IndexOfInvariant(start.Index, end.Index); 
                if (edge != -1)
                {
                    SetConstraint(ti, edge, type);
                    return true;
                }

            } while (walker.Next());

            return false;
        }

        public void SetConstraint(int triangle, int edge, int type)
        {
            List<Triangle> tris = Triangles;
            Triangle t = tris[triangle].Orient(edge);
            t.con0 = type;

            int adjIndex = t.adj0;
            if (adjIndex != -1)
            {
                Triangle adj = tris[adjIndex].Orient(t.vtx1, t.vtx0);
                adj.con0 = type;
                tris[adjIndex] = adj;
            }
            tris[t.index] = t;
        }

        public bool CanFlip(int triangle, int edge, out bool should)
        {
            should = false;

            List<Node> nodes = Nodes;
            List<Triangle> tris = Triangles;
            Triangle t0 = tris[triangle].Orient(edge);
            if (t0.adj0 == -1 || t0.con0 != -1)
            {
                return false;
            }

            Node v0 = nodes[t0.vtx0];
            Node v1 = nodes[t0.vtx1];
            Node v2 = nodes[t0.vtx2];

            Triangle t1 = tris[t0.adj0].Orient(v1.Index, v0.Index);
            Node v3 = nodes[t1.vtx2];
             
            should = Circles[triangle].Contains(v3.X, v3.Y);
            return GeometryHelper.IsConvex(v1, v2, v0, v3);
        }

        public (int t, int e, int v) FindContaining(double x, double y, double eps, List<int>? path = null, int searchStart = -1)
        {
            List<Triangle> tris = Triangles;
            int n = tris.Count;
            if (n == 0)
            {
                return (-1, -1, -1);
            }

            int current = searchStart == -1 ? n - 1 : searchStart;
            int maxSteps = n * 3;
            int steps = 0;

            List<Node> nodes = Nodes;
            Node vertex = new Node(x, y, -1);
            while (steps++ < maxSteps)
            {
                path?.Add(current);

                Triangle t = tris[current];
                Node v0 = nodes[t.vtx0];
                Node v1 = nodes[t.vtx1];
                Node v2 = nodes[t.vtx2];

                double cross01 = GeometryHelper.Cross(v0, v1, vertex);
                double cross12 = GeometryHelper.Cross(v1, v2, vertex);
                double cross20 = GeometryHelper.Cross(v2, v0, vertex);

                int bestExit = t.adj0;
                int worstEdge = 0;
                double worstCross = cross01;

                if (cross12 < worstCross)
                {
                    worstCross = cross12;
                    bestExit = t.adj1;
                    worstEdge = 1;
                }

                if (cross20 < worstCross)
                {
                    worstCross = cross20;
                    bestExit = t.adj2;
                    worstEdge = 2;
                }

                if (Math.Abs(worstCross) <= eps)
                {
                    (int si, int ei) = t.Edge(worstEdge);

                    Node start = nodes[si];
                    if (Node.CloseOrEqual(start, vertex, eps))
                    {
                        return (current, -1, si);
                    }

                    Node end = nodes[ei];
                    if (Node.CloseOrEqual(end, vertex, eps))
                    {
                        return (current, -1, ei);
                    }

                    if (new Rectangle(start, end).Contains(x, y))
                    {
                        return (current, worstEdge, -1);
                    }
                }

                if (worstCross > 0)
                {
                    return (current, -1, -1);
                }
                current = bestExit;
            }
            return (-1, -1, -1);
        }

        public (int t, int e) FindEdge(Node start, Node end, bool invariant)
        {
            List<Triangle> triangles = Triangles;
            Circler circler = new Circler(triangles, start);

            int ai = start.Index, bi = end.Index;
            do
            {
                int index = circler.Current;
                Triangle t = triangles[index];

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
                Node center = new Node(circle.x, circle.y, -1);
                Node pt = new Node(x, y, -1);
                foreach (var s in segments)
                {
                    if (this.Equals(s))
                        continue;

                    if (GeometryHelper.Intersect(center, pt, s.start, s.end, out _, out _))
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
