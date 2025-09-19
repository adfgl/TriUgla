using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public class Finder
    {
        public (int t, int e) FindEdge(Mesh mesh, Vertex start, Vertex end, bool invariant)
        {
            List<Triangle> tris = mesh.Triangles;
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

        public (int t, int e, int v) FindContaining(Mesh mesh, List<int> path, double x, double y, double eps, int searchStart = -1)
        {
            List<Triangle> tris = mesh.Triangles;
            List<Vertex> verts = mesh.Vertices;
            if (tris.Count == 0 || !mesh.Bounds.Contains(x, y))
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
