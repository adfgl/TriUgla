namespace TriUgla
{
    public class Mesh
    {
        public Rectangle Bounds { get; set; }
        public List<Vertex> Vertices { get; set; } = new List<Vertex>();
        public List<Circle> Circles { get; set; } = new List<Circle>();
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();

        public void SetAdjacent(int parent, int parentStart, int parentEnd, int child)
        {
            if (parent == -1) return;

            Triangle t = Triangles[parent].Orient(parentStart, parentEnd);

            t.adj0 = child;
            Triangles[parent] = t;
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
    }
}
