using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public class Splitter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAdjacent(Mesh mesh, int parent, int parentStart, int parentEnd, int child)
        {
            if (parent == -1) return;

            List<Triangle> tris = mesh.Triangles;
            Triangle t = tris[parent].Orient(parentStart, parentEnd);

            t.adj0 = child;
            tris[parent] = t;
        }

        public int[] Split(Mesh mesh, int triangleIndex, int vertexIndex)
        {
            List<Triangle> triangles = mesh.Triangles;
            List<Vertex> vertices = mesh.Vertices;
            List<Circle> circles = mesh.Circles;

            Triangle old = triangles[triangleIndex];

            int i0 = old.vtx0;
            int i1 = old.vtx1;
            int i2 = old.vtx2;
            int i3 = vertexIndex;

            Vertex v0 = vertices[i0];
            Vertex v1 = vertices[i1];
            Vertex v2 = vertices[i2];
            Vertex v3 = vertices[i3];

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

            return [t0, t1, t2];
        }
    }
}
