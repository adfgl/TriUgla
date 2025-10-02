namespace TriUgla
{
    public class Legalizer : MeshHelperBase
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        public Legalizer(Mesh mesh) : base(mesh)
        {
        }

        /// <summary>
        /// Legalizes newly created triangles by edge-flipping until Delaunay criteria
        /// (or your <c>CanFlip</c> rule) is satisfied. Starts from the triangles in
        /// <paramref name="created"/>, pushes affected triangles to <paramref name="affected"/>,
        /// and returns the total number of flips performed.
        /// </summary>
        /// <param name="mesh">Mesh to modify (triangles/circles/adjacency updated in-place).</param>
        /// <param name="created">Buffer containing seed triangle indices to process.</param>
        /// <param name="count">Number of valid entries in <paramref name="created"/>.</param>
        /// <param name="affected">Stack collecting triangles touched during legalization.</param>
        /// <returns>Total number of edge flips executed.</returns>
        public int Legalize(int[] created, int count, Stack<int>? affected = null)
        {
            Stack<int> stack = new Stack<int>();
            for (int i = 0; i < count; i++) stack.Push(created[i]);

            int flips = 0;
            List<Triangle> tris = _mesh.Triangles;
            Dictionary<(int a, int b), int> flipCount = new Dictionary<(int a, int b), int>(64);
            while (stack.Count > 0)
            {
                int t = stack.Pop();
                if (affected is not null)
                {
                    affected.Push(t);
                }

                for (int edge = 0; edge < 3; edge++)
                {
                    Triangle tri = tris[t];
                    var (u, v) = tri.Edge(edge);
                    if (u > v) { int tmp = u; u = v; v = tmp; }

                    if (flipCount.TryGetValue((u, v), out int c) && c >= MAX_FLIPS_PER_DIAGONAL)
                        continue;

                    if (CanFlip(t, edge, out bool should) && should)
                    {
                        flips++;
                        flipCount[(u, v)] = c + 1;

                        count = Flip(m_created, t, edge, false);
                        for (int i = 0; i < count; i++)
                        {
                            int idx = m_created[i];
                            stack.Push(idx);
                            if (t != idx && affected is not null)
                            {
                                affected.Push(idx);
                            }
                        }
                        stack.Push(t);
                        break;
                    }
                }
            }

            return flips;
        }

        public bool CanFlip(int triangle, int edge, out bool should)
        {
            should = false;
            List<Triangle> tris = _mesh.Triangles;
            List<Node> verts = _mesh.Nodes;

            Triangle t0 = tris[triangle].Orient(edge);
            if (t0.adj0 == -1 || t0.con0 != -1) return false;

            Node v0 = verts[t0.vtx0];
            Node v1 = verts[t0.vtx1];
            Node v2 = verts[t0.vtx2];

            Triangle t1 = tris[t0.adj0].Orient(v1.Index, v0.Index);
            Node v3 = verts[t1.vtx2];

            should = _mesh.Circles[triangle].Contains(v3.X, v3.Y);
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
            List<Triangle> triangles = _mesh.Triangles;
            List<Node> vertices = _mesh.Nodes;
            List<Circle> circles = _mesh.Circles;

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

            _mesh.SetAdjacent(old0.adj1, i2, i1, t1);
            _mesh.SetAdjacent(old1.adj1, i3, i0, t0);

            v0.Triangle = v2.Triangle = v3.Triangle = t0;
            v1.Triangle = t1;

            m_created[0] = t0;
            m_created[1] = t1;
            return 2;
        }
    }
}
