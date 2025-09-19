using System.Runtime.CompilerServices;

namespace TriUgla
{
    public class Splitter
    {
        /// <summary>
        /// Splits an existing triangle in the mesh into three smaller triangles by inserting
        /// a new vertex inside it. Updates the triangle and circle lists in the mesh and
        /// returns the indices of the newly created triangles.
        /// </summary>
        /// <param name="newTris">
        /// Output buffer that will contain the indices of the three triangles produced
        /// by the split operation. Must be at least length 4.
        /// </param>
        /// <param name="mesh">Target mesh whose triangle, vertex, and circle lists will be modified.</param>
        /// <param name="triangleIndex">Index of the triangle in <see cref="Mesh.Triangles"/> to split.</param>
        /// <param name="vertexIndex">Index of the vertex in <see cref="Mesh.Vertices"/> to insert.</param>
        /// <returns>The number of triangles written into <paramref name="newTris"/> (always 4).</returns>
        public static int Split(int[] newTris, Mesh mesh, int triangleIndex, int vertexIndex)
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

            mesh.SetAdjacent(old.adj1, i2, i1, t1);
            mesh.SetAdjacent(old.adj2, i0, i2, t2);

            v0.Triangle = v1.Triangle = v3.Triangle = t0;
            v2.Triangle = t1;

            newTris[0] = t0;
            newTris[1] = t1;
            newTris[2] = t2;
            return 3;
        }

        /// <summary>
        /// Splits a triangle by inserting a vertex on (or opposite) a given edge, updating mesh
        /// connectivity and circumcircles in-place. If the edge is a boundary, the triangle is
        /// split into two; if it has an adjacent triangle, the adjacent pair is split into four.
        /// </summary>
        /// <param name="newTris">
        /// Output buffer for written triangle indices. For boundary splits, the method writes
        /// 2 indices; for interior splits, it writes 4. The array length must be ≥ 4.
        /// </param>
        /// <param name="mesh">Target mesh (its <see cref="Mesh.Triangles"/> and <see cref="Mesh.Circles"/> are modified).</param>
        /// <param name="triangleIndex">Index into <see cref="Mesh.Triangles"/> of the triangle to split.</param>
        /// <param name="edgeIndex">
        /// Oriented local edge index of <paramref name="triangleIndex"/> to use as the base (0,1,2).  
        /// The triangle is first oriented so that this edge becomes (v0–v1).
        /// </param>
        /// <param name="vertexIndex">Index into <see cref="Mesh.Vertices"/> of the inserted vertex.</param>
        /// <returns>
        /// The number of triangle indices written to <paramref name="newTris"/>: 2 for boundary,
        /// or 4 for interior split.
        /// </returns>
        public static int Split(int[] newTris, Mesh mesh, int triangleIndex, int edgeIndex, int vertexIndex)
        {
            List<Triangle> triangles = mesh.Triangles;
            List<Vertex> vertices = mesh.Vertices;
            List<Circle> circles = mesh.Circles;

            Triangle old0 = triangles[triangleIndex].Orient(edgeIndex);
            int con = old0.con0;

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;

            Vertex v0 = vertices[i0];
            Vertex v1 = vertices[i1];
            Vertex v2 = vertices[i2];
            Vertex v3 = vertices[i3];

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

                mesh.SetAdjacent(old0.adj1, i2, i1, t1);

                v0.Triangle = v2.Triangle = v3.Triangle = t0;
                v1.Triangle = t1;

                newTris[0] = t0;
                newTris[1] = t1;
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
                Vertex v4 = vertices[i4];

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

                mesh.SetAdjacent(old0.adj1, i2, i1, t3);
                mesh.SetAdjacent(old1.adj2, i1, i3, t2);

                v0.Triangle = v2.Triangle = t0;
                v1.Triangle = t3;
                v3.Triangle = t1;

                newTris[0] = t0;
                newTris[1] = t1;
                newTris[2] = t2;
                newTris[3] = t3;
                return 4;
            }
        }
    }
}
