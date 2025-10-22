namespace TriScript.Data.Units
{
    public readonly struct Dimension : IEquatable<Dimension>
    {
        public readonly int l;
        public Dimension(int l) => this.l = l;

        public static readonly Dimension None = new(0);
        public static readonly Dimension Length = new(1);

        public static Dimension operator +(Dimension a, Dimension b) => new(a.l + b.l);
        public static Dimension operator -(Dimension a, Dimension b) => new(a.l - b.l);
        public Dimension Pow(int p) => new(checked(l * p));

        public bool Equals(Dimension other) => l == other.l;
        public override bool Equals(object? obj) => obj is Dimension d && Equals(d);
        public override int GetHashCode() => l.GetHashCode();
        public override string ToString() => l switch { 0 => "1", 1 => "L", _ => $"L^{l}" };
    }
}
