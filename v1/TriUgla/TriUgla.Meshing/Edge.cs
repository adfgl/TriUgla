namespace TriUgla.Meshing
{
    public readonly struct Edge : IEquatable<Edge>
    {
        public readonly int start, end;

        public Edge(int start, int end)
        {
            if (start < end)
            {
                this.start = start;
                this.end = end;
            }
            else
            {
                this.start = end;
                this.end = start;
            }
        }

        public override int GetHashCode() => HashCode.Combine(start, end);

        public bool Equals(Edge other)
        {
            return start == other.start && end == other.end;
        }

        public override bool Equals(object? obj)
        {
            return obj is Edge other && Equals(other);
        }
    }
}
