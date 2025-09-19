using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public class Mesh
    {
        public List<Vertex> Vertices { get; set; }
        public List<Circle> Circles { get; set; }
        public List<Triangle> Triangles { get; set; }

        public int[] Split(int triangleIndex, int vertexIndex)
        {
            List<Triangle> tris = Triangles;
            List<Vertex> vrts = Vertices;
            List<Circle> circles = Circles;

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
        }
    }
}
