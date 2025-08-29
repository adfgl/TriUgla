using GeometryLib;
using System.Runtime.CompilerServices;

namespace Triangulation
{
    public static class CDT
    {
        public static bool ShouldRemove(Mesh mesh, int[] contour, HashSet<(int, int)> edges)
        {
            List<Triangle> tris = mesh.Triangles;
            for (int i = 0; i < tris.Count; i++)
            {
                Triangle t = tris[i];
                if (t.removed)
                {
                    continue;
                }

                if (ContainsSuperVertex(t, 3))
                {
                    t.removed = true;
                    tris[i] = t;
                    continue;
                }

                int i0 = t.vtx0;
                int i1 = t.vtx1;
                int i2 = t.vtx2;
            }




            return false;
        }





        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsSuperVertex(Triangle t, int super)
        {
            return t.vtx0 < super || t.vtx1 < super || t.vtx2 < super;
        }





        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(Vertex a, Vertex b, Vertex c)
        {
            return Vertex.Cross(a, b, c) * 0.5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CCWQuadConvex(Vertex a, Vertex b, Vertex c, Vertex d)
        {
            return
                Vertex.Cross(a, b, c) > 0 &&
                Vertex.Cross(b, c, d) > 0 &&
                Vertex.Cross(c, d, a) > 0 &&
                Vertex.Cross(d, a, b) > 0;
        }



        public class Vertex : IPoint
        {
            public Vertex(double x, double y)
            {
                X = x;
                Y = y;
            }

            public int Index { get; set; } = -1;
            public int Triangle { get; set; } = -1;
            public double X { get; set; }
            public double Y { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double SquareLength(Vertex a, Vertex b)
            {
                double dx = a.X - b.X;
                double dy = a.Y - b.Y;
                return dx * dx + dy * dy;
            }

            public static double Length(Vertex a, Vertex b) => Math.Sqrt(SquareLength(a, b));

            public static bool CloseOrEqual(Vertex a, Vertex b, double eps)
            {
                if (a.Index == b.Index && a.Index != -1)
                {
                    return true;
                }
                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                return dx * dx + dy * dy <= eps;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Cross(Vertex a, Vertex b, Vertex c)
            {
                return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            }

            public override string ToString()
            {
                return $"[{Index}] {X} {Y}";
            }
        }

        public class Mesh
        {
            readonly Stack<int> _affected = new Stack<int>();

            public Rectangle Bounds { get; set; }
            public List<Triangle> Triangles { get; set; }
            public List<Vertex> Vertices { get; set; }

            public Vertex Insert(int[] created, int count, int triangle, int edge, double x, double y, double eps)
            {
                int vi = Vertices.Count;
                Vertex vtx = new Vertex(x, y) { Index = vi, Triangle = -1 };
                Vertices.Add(vtx);

                if (edge == -1)
                {
                    count = Split(created, triangle, vi);
                }
                else
                {
                    count = Split(created, triangle, edge, vi);
                }
                Legalize(_affected, created, count);
                return vtx;
            }

            public void Legalize(Stack<int> affected, int[] created, int count)
            {
                Stack<int> stack = new Stack<int>();
                for (int i = 0; i < count; i++) stack.Push(created[i]);

                const int MAX_FLIPS_PER_DIAGONAL = 5;

                List<Triangle> tris = Triangles;
                Dictionary<(int a, int b), int> flipCount = new Dictionary<(int a, int b), int>();

                while (stack.Count > 0)
                {
                    int t = stack.Pop();
                    affected.Push(t);

                    for (int edge = 0; edge < 3; edge++)
                    {
                        Triangle tri = tris[t];
                        var (u, v) = tri.Edge(edge);
                        var key = (u < v) ? (u, v) : (v, u);

                        if (flipCount.TryGetValue(key, out int c) && c >= MAX_FLIPS_PER_DIAGONAL)
                            continue;

                        if (CanFlip(t, edge, out bool should) && should)
                        {
                            flipCount[key] = c + 1;

                            count = Flip(created, t, edge);
                            for (int i = 0; i < count; i++)
                            {
                                int idx = created[i];
                                stack.Push(idx);
                                if (t != idx) affected.Push(idx);
                            }
                            stack.Push(t);
                            break;
                        }
                    }
                }
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
                List<Triangle> tris = Triangles;
                List<Vertex> verts = Vertices;

                Triangle t0 = tris[triangle].Orient(edge);
                if (t0.adj0 == -1 || t0.con0 != -1) return false;

                Vertex v0 = verts[t0.vtx0];
                Vertex v1 = verts[t0.vtx1];
                Vertex v2 = verts[t0.vtx2];

                Triangle t1 = tris[t0.adj0].Orient(v1.Index, v0.Index);
                Vertex v3 = verts[t1.vtx2];

                should = t0.circle.Contains(v3.X, v3.Y);
                return CCWQuadConvex(v1, v2, v0, v3);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SetAdjacent(Mesh mesh, int parent, int parentStart, int parentEnd, int child)
            {
                if (parent == -1) return;

                List<Triangle> tris = mesh.Triangles;
                Triangle t = tris[parent].Orient(parentStart, parentEnd);

                t.adj0 = child;
                tris[parent] = t;
            }

            public int Flip(int[] newIndices, int triangleIndex, int edgeIndex)
            {
                List<Triangle> tris = Triangles;
                List<Vertex> verts = Vertices;

                Triangle old0 = tris[triangleIndex].Orient(edgeIndex);
                if (old0.con0 != -1 || old0.adj0 == -1)
                {
                    throw new Exception("Unable to flip edge.");
                }

                int i0 = old0.vtx0;
                int i1 = old0.vtx1;
                int i2 = old0.vtx2;

                Triangle old1 = tris[old0.adj0].Orient(i1, i0);
                int i3 = old1.vtx2;

                int t0 = old0.index;
                int t1 = old1.index;

                Vertex v0 = verts[i0];
                Vertex v1 = verts[i1];
                Vertex v2 = verts[i2];
                Vertex v3 = verts[i3];

                // the first diagonal MUST be opposite to v2 to avoid degeneracy
                // 1) 0-3-2
                // 2) 3-1-2

                bool removed = old0.removed && old1.removed;

                double a0 = Area(v0, v3, v2);
                Circle c0 = new Circle(v0, v3, v2);
                tris[t0] = new Triangle(t0,
                    i0, i3, i2,
                    old1.adj1, t1, old0.adj2,
                    old1.con1, -1, old0.con2,
                    c0, a0, removed);

                double a1 = old0.area + old1.area - a0;
                Circle c1 = new Circle(v3, v1, v2);
                tris[t1] = new Triangle(t1,
                    i3, i1, i2,
                    old1.adj2, old0.adj1, t0,
                    old1.con2, old0.con1, -1,
                    c1, a1, removed);

                SetAdjacent(this, old0.adj1, i2, i1, t1);
                SetAdjacent(this, old1.adj1, i3, i0, t0);

                v0.Triangle = v2.Triangle = v3.Triangle = t0;
                v1.Triangle = t1;

                newIndices[0] = t0;
                newIndices[1] = t1;
                return 2;
            }

            /// <summary>
            /// Splits triangle edge into 2 or 4 triangles and updates <paramref name="mesh"/>.
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="triangleIndex">Index of triangle to split.</param>
            /// <param name="edgeIndex">Index of triangle edge to split.</param>
            /// <param name="vertexIndex">Index of vertex to split with.</param>
            /// <returns>Number of new indices of resulting triangles.</returns>
            public int Split(int[] newIndices, int triangleIndex, int edgeIndex, int vertexIndex)
            {
                List<Triangle> tris = Triangles;
                List<Vertex> vrts = Vertices;

                Triangle old0 = tris[triangleIndex].Orient(edgeIndex);
                int con = old0.con0;

                int i0 = old0.vtx0;
                int i1 = old0.vtx1;
                int i2 = old0.vtx2;
                int i3 = vertexIndex;

                Vertex v0 = vrts[i0];
                Vertex v1 = vrts[i1];
                Vertex v2 = vrts[i2];
                Vertex v3 = vrts[i3];

                int adj = old0.adj0;
                if (adj == -1)
                {
                    int t0 = old0.index;
                    int t1 = tris.Count;

                    // 1) 2-0-3
                    // 2) 1-2-3

                    double a0 = Area(v2, v0, v3);
                    Circle c0 = new Circle(v2, v0, v3);
                    tris[t0] = new Triangle(t0,
                        i0, i1, i3,
                        old0.adj2, -1, t1,
                        old0.con2, con, -1,
                        c0, a0, old0.removed);

                    double a1 = old0.area - a0;
                    Circle c1 = new Circle(v2, v1, v3);
                    tris.Add(new Triangle(t1,
                        i2, i1, i3,
                        old0.adj1, t0, -1,
                        old0.con1, -1, con,
                        c1, a1, old0.removed));

                    SetAdjacent(this, old0.adj1, i2, i1, t1);

                    v0.Triangle = v2.Triangle = v3.Triangle = t0;
                    v1.Triangle = t1;

                    newIndices[0] = t0;
                    newIndices[1] = t1;
                    return 2;
                }
                else
                {
                    Triangle old1 = tris[adj].Orient(i1, i0);

                    int t0 = old0.index;
                    int t1 = old1.index;
                    int t2 = tris.Count;
                    int t3 = t2 + 1;

                    int i4 = old1.vtx2;
                    Vertex v4 = vrts[i4];

                    // 1) 2-0-3
                    // 2) 0-4-3
                    // 3) 4-1-3
                    // 4) 1-2-3

                    double a0 = Area(v2, v0, v3);
                    Circle c0 = new Circle(v2, v0, v3);
                    tris[t0] = new Triangle(t0,
                        i2, i0, i3,
                        old0.adj2, t1, t3,
                        old0.con2, con, -1,
                        c0, a0, old0.removed);

                    double a1 = Area(v0, v4, v3);
                    Circle c1 = new Circle(v0, v4, v3);
                    tris[t1] = new Triangle(t1,
                        i0, i4, i3,
                        old1.adj1, t2, t0,
                        old1.con1, -1, con,
                        c1, a1, old1.removed);

                    double a2 = old1.area - a1;
                    Circle c2 = new Circle(v4, v1, v3);
                    tris.Add(new Triangle(t2,
                        i4, i1, i3,
                        old0.adj2, t3, t1,
                        old0.con2, con, -1,
                        c2, a2, old1.removed));

                    double a3 = old0.area - a0;
                    Circle c3 = new Circle(v1, v2, v3);
                    tris.Add(new Triangle(t3,
                        i1, i2, i3,
                        old0.adj1, t0, t2,
                        old0.con1, -1, con,
                        c3, a3, old0.removed));

                    SetAdjacent(this, old0.adj1, i2, i1, t3);
                    SetAdjacent(this, old1.adj2, i1, i3, t2);

                    newIndices[0] = t0;
                    newIndices[1] = t1;
                    newIndices[2] = t2;
                    newIndices[3] = t3;
                    return 4;
                }
            }

            /// <summary>
            /// Splits triangle into 3 new triangles and updates <paramref name="mesh"/>.
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="triangleIndex">Index of triangle to split.</param>
            /// <param name="vertexIndex">Index of vertex to split with.</param>
            /// <returns>Indices of resulting triangles.</returns>
            public int Split(int[] newIndices, int triangleIndex, int vertexIndex)
            {
                List<Triangle> tris = Triangles;
                List<Vertex> vrts = Vertices;

                Triangle old = tris[triangleIndex];

                int i0 = old.vtx0;
                int i1 = old.vtx1;
                int i2 = old.vtx2;
                int i3 = vertexIndex;

                Vertex v0 = vrts[i0];
                Vertex v1 = vrts[i1];
                Vertex v2 = vrts[i2];
                Vertex v3 = vrts[i3];

                // 1) 0-1-3
                // 2) 1-2-3
                // 3) 2-0-3
                int t0 = old.index;
                int t1 = tris.Count;
                int t2 = t1 + 1;

                double a0 = Area(v0, v1, v3);
                Circle c0 = new Circle(v0, v1, v3);
                tris[t0] = new Triangle(t0,
                    i0, i1, i3,
                    old.adj0, t1, t2,
                    old.con0, -1, -1,
                    c0, a0, old.removed);

                double a1 = Area(v1, v2, v3);
                Circle c1 = new Circle(v1, v2, v3);
                tris.Add(new Triangle(t1,
                    i1, i2, i3,
                    old.adj1, t2, t0,
                    old.con1, -1, -1,
                    c1, a1, old.removed));

                double a2 = old.area - a0 - a1;
                Circle c2 = new Circle(v2, v0, v3);
                tris.Add(new Triangle(t2,
                    i2, i0, i3,
                    old.adj2, t0, t1,
                    old.con2, -1, -1,
                    c2, a2, old.removed));

                SetAdjacent(this, old.adj1, i2, i1, t1);
                SetAdjacent(this, old.adj2, i0, i2, t2);

                v0.Triangle = v1.Triangle = v3.Triangle = t0;
                v2.Triangle = t1;

                newIndices[0] = t0;
                newIndices[1] = t1;
                newIndices[2] = t2;
                return 3;
            }

            public (int t, int e) FindEdge(Vertex start, Vertex end, bool invariant)
            {
                List<Triangle> tris = Triangles;
                Circler circler = new Circler(tris, start);

                int ai = start.Index;
                int bi = end.Index;
                do
                {
                    int index = circler.Current;
                    Triangle t = tris[index];

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

            public (int t, int e, int v) FindContaining(List<int> path, double x, double y, double eps, int searchStart = -1)
            {
                List<Triangle> tris = Triangles;
                List<Vertex> verts = Vertices;
                if (tris.Count == 0 || !Bounds.Contains(x, y))
                {
                    return (-1, -1, -1);
                }

                int current = searchStart == -1 ? tris.Count - 1 : searchStart;
                int maxSteps = tris.Count * 3;
                int trianglesChecked = 0;

                Vertex vertex = new Vertex(x, y);
                while (trianglesChecked++ < maxSteps)
                {
                    path.Add(current);

                    Triangle t = tris[current];
                    Vertex v0 = verts[t.vtx0];
                    Vertex v1 = verts[t.vtx1];
                    Vertex v2 = verts[t.vtx2];

                    double cross01 = Vertex.Cross(v0, v1, vertex);
                    double cross12 = Vertex.Cross(v1, v2, vertex);
                    double cross20 = Vertex.Cross(v2, v0, vertex);

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

                        Vertex start = verts[si];
                        if (Vertex.CloseOrEqual(start, vertex, eps))
                        {
                            return (current, -1, si);
                        }

                        Vertex end = verts[ei];
                        if (Vertex.CloseOrEqual(end, vertex, eps))
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
        }

        public struct Edge
        {

        }

        public struct Triangle
        {
            public int index;
            public int vtx0, vtx1, vtx2;
            public int adj0, adj1, adj2;
            public int con0, con1, con2;

            public Circle circle;
            public double area;
            public bool removed;

            public Triangle(
                int index,
                int vtx0, int vtx1, int vtx2,
                int adj0, int adj1, int adj2,
                int con0, int con1, int con2,

                Circle circle, double area, bool removed)
            {
                this.index = index;
                this.vtx0 = vtx0; this.vtx1 = vtx1; this.vtx2 = vtx2;
                this.adj0 = adj0; this.adj1 = adj1; this.adj2 = adj2;
                this.con0 = con0; this.con1 = con1; this.con2 = con2;

                this.circle = circle;
                this.area = area;
                this.removed = removed;
            }

            public override string ToString()
            {
                return $"[{index}] {vtx0} {vtx1} {vtx2}";
            }

            public int IndexOf(int vertex)
            {
                if (vertex == vtx0) return 0;
                if (vertex == vtx1) return 1;
                if (vertex == vtx2) return 2;
                return -1;
            }

            public int IndexOf(int start, int end)
            {
                if (start == vtx0 && end == vtx1) return 0;
                if (start == vtx1 && end == vtx2) return 1;
                if (start == vtx2 && end == vtx0) return 2;
                return -1;
            }

            public (int start, int end) Edge(int index)
            {
                return index switch
                {
                    0 => (vtx0, vtx1),
                    1 => (vtx1, vtx2),
                    2 => (vtx2, vtx0),
                    _ => throw new IndexOutOfRangeException($"Expected index 0, 1 or 2 but got {index}."),
                };
            }

            public Triangle Orient(int edge)
            {
                return edge switch
                {
                    0 => this,

                    1 => new Triangle(
                        index,

                        vtx1, vtx2, vtx0,
                        adj1, adj2, adj0,
                        con1, con2, con0,

                        circle, area, removed),

                    2 => new Triangle(
                        index,

                        vtx2, vtx0, vtx1,
                        adj2, adj0, adj1,
                        con2, con0, con1,

                        circle, area, removed),

                    _ => throw new IndexOutOfRangeException($"Expected index 0, 1 or 2 but got {edge}."),
                };
            }

            public Triangle Orient(int start, int end) => Orient(IndexOf(start, end));
        }

        public struct Circler
        {
            readonly List<Triangle> _triangles;
            readonly int _start, _vertex;
            int _current;

            public Circler(List<Triangle> triangles, int triangleIndex, int vertex)
            {
                _triangles = triangles;
                _vertex = vertex;
                _start = triangleIndex;
                _current = triangleIndex;
            }

            public Circler(List<Triangle> triangles, Vertex vtx) : this(triangles, vtx.Triangle, vtx.Index)
            {

            }

            public int Current => _current;
            public int Vertex => _vertex;

            public bool Next()
            {
                Triangle t = _triangles[_current];
                t = t.Orient(t.IndexOf(_vertex));
                int next = t.adj0;
                if (next == _start || next == -1)
                {
                    return false;
                }
                _current = next;
                return true;
            }
        }
    }
}
