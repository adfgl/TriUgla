using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public class Mesh
    {
        public Rectangle Bounds { get; set; }
        public List<Vertex> Vertices { get; set; }
        public List<Circle> Circles { get; set; }
        public List<Triangle> Triangles { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAdjacent(int parent, int parentStart, int parentEnd, int child)
        {
            if (parent == -1) return;

            Triangle t = Triangles[parent].Orient(parentStart, parentEnd);

            t.adj0 = child;
            Triangles[parent] = t;
        }
    }
}
