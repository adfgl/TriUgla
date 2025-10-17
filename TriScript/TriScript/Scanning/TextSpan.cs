namespace TriScript.Scanning
{
    public readonly struct TextSpan : IEquatable<TextSpan>
    {
        public readonly int start, length;

        public TextSpan(int start, int length)
        {
            this.start = start;
            this.length = length;
        }

        public bool Equals(TextSpan other) 
            => start == other.start && length == other.length;

        public override bool Equals(object? obj)
            => obj is TextSpan other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(start, length);

        public static bool operator ==(TextSpan left, TextSpan right)
            => left.Equals(right);

        public static bool operator !=(TextSpan left, TextSpan right)
            => !left.Equals(right);

        public override string ToString()
            => $"[{start}..{start + length})";
    }
}
