using System.Runtime.CompilerServices;

namespace TriUgla.Meshing
{
    public enum ETriangleState : byte
    {
        Keep, Remove, Ambiguous
    }

    public struct Triangle
    {
        public static readonly Triangle Dead = new Triangle(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, ETriangleState.Ambiguous);

        public int index;
        public int vtx0, vtx1, vtx2;
        public int adj0, adj1, adj2;
        public int con0, con1, con2;
        public ETriangleState state;

        public Triangle(
            int index,
            int vtx0, int vtx1, int vtx2,
            int adj0, int adj1, int adj2,
            int con0, int con1, int con2,

            ETriangleState state)
        {
            this.index = index;
            this.vtx0 = vtx0; this.vtx1 = vtx1; this.vtx2 = vtx2;
            this.adj0 = adj0; this.adj1 = adj1; this.adj2 = adj2;
            this.con0 = con0; this.con1 = con1; this.con2 = con2;
            this.state = state;
        }

        public override string ToString()
        {
            return $"[{index}] {vtx0} {vtx1} {vtx2}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int v)
        {
            if (v == vtx0) return 0;
            if (v == vtx1) return 1;
            return v == vtx2 ? 2 : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int s, int e)
        {
            if (s == vtx0) return e == vtx1 ? 0 : -1;
            if (s == vtx1) return e == vtx2 ? 1 : -1;
            if (s == vtx2) return e == vtx0 ? 2 : -1;
            return -1;
        }

        public int IndexOfInvariant(int start, int end)
        {
            int edge = IndexOf(start, end);
            if (edge == -1) edge = IndexOf(end, start);
            return edge;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Edge(int i, out int s, out int e)
        {
            switch (i)
            {
                case 0: s = vtx0; e = vtx1; return;
                case 1: s = vtx1; e = vtx2; return;
                case 2: s = vtx2; e = vtx0; return;
                default: s = e = -1; return; 
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    
                    state),

                2 => new Triangle(
                    index,

                    vtx2, vtx0, vtx1,
                    adj2, adj0, adj1,
                    con2, con0, con1,

                    state),

                _ => Dead,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Triangle Orient(int start, int end)
        {
            if (start == vtx0 && end == vtx1)
            {
                return this;
            }

            if (start == vtx1 && end == vtx2) 
            {
                return new Triangle(
                    index,

                    vtx1, vtx2, vtx0,
                    adj1, adj2, adj0,
                    con1, con2, con0,

                    state);
            }

            if (start == vtx2 && end == vtx0)
            {
                return new Triangle(
                    index,

                    vtx2, vtx0, vtx1,
                    adj2, adj0, adj1,
                    con2, con0, con1,

                    state);
            }
            return Dead;
        }
    }
}
