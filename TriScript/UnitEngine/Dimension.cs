namespace UnitEngine
{
    public readonly struct Dimension : IEquatable<Dimension>
    {
        /// <summary>
        /// length
        /// </summary>
        public readonly sbyte l;

        public static readonly Dimension None = new Dimension(0);
        public static readonly Dimension Length = new Dimension(1);


        public Dimension(sbyte l)
        {
            this.l = l;
        }

        public static Dimension operator +(Dimension a, Dimension b)
            => new Dimension((sbyte)(a.l + b.l));

        public static Dimension operator -(Dimension a, Dimension b)
            => new Dimension((sbyte)(a.l - b.l));

        public Dimension Pow(int p)
            => new Dimension((sbyte)(l * p));

        public bool Equals(Dimension other) => l == other.l;
        public override bool Equals(object? obj) => obj is Dimension d && Equals(d);
        public override int GetHashCode() => l.GetHashCode();

        public static bool AreCompatible(Dimension a, Dimension b) => a.Equals(b);

        public override string ToString()
        {
            return l switch
            {
                0 => "1",
                1 => "L",
                _ => $"L^{l}"
            };
        }
    }
}
