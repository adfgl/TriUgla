namespace TriUgla
{
    public struct Triangle
    {
        public int index;
        public int vtx0, vtx1, vtx2;
        public int adj0, adj1, adj2;
        public int con0, con1, con2;

        public Triangle(
            int index,
            int vtx0, int vtx1, int vtx2,
            int adj0, int adj1, int adj2,
            int con0, int con1, int con2)
        {
            this.index = index;
            this.vtx0 = vtx0; this.vtx1 = vtx1; this.vtx2 = vtx2;
            this.adj0 = adj0; this.adj1 = adj1; this.adj2 = adj2;
            this.con0 = con0; this.con1 = con1; this.con2 = con2;
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
                    con1, con2, con0),

                2 => new Triangle(
                    index,

                    vtx2, vtx0, vtx1,
                    adj2, adj0, adj1,
                    con2, con0, con1),

                _ => throw new IndexOutOfRangeException($"Expected index 0, 1 or 2 but got {edge}."),
            };
        }

        public Triangle Orient(int start, int end) => Orient(IndexOf(start, end));
    }
}
