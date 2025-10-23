using System.Runtime.CompilerServices;

namespace TriScript.UnitHandling
{
    public readonly struct DimEval
    {
        public readonly double scale; 
        public readonly Dim dim;

        public readonly static DimEval None = new DimEval(Dim.None, 1);

        public DimEval(Dim dim, double scale = 1.0)
        {
            this.dim = dim;
            this.scale = scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DimEval Mul(in DimEval a, in DimEval b)
            => new DimEval(Dim.Sum(a.dim, b.dim), a.scale * b.scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DimEval Div(in DimEval a, in DimEval b)
            => new DimEval(Dim.Div(a.dim, b.dim), a.scale / b.scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DimEval Pow(in DimEval a, int p)
            => new DimEval(Dim.Pow(a.dim, p), Math.Pow(a.scale, p));

        public static DimEval Add(in DimEval a, in DimEval b)
        {
            EnsureSame(a.dim, b.dim, "add");
            return new DimEval(a.dim, a.scale + b.scale);
        }

        public static DimEval Sub(in DimEval a, in DimEval b)
        {
            EnsureSame(a.dim, b.dim, "sub");
            return new DimEval(a.dim, a.scale - b.scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSame(in Dim a, in Dim b, string op)
        {
            if (!a.Equals(b))
                throw new InvalidOperationException($"{op}: dimensions mismatch: {a} vs {b}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureDimensionless(in Dim d, string where)
        {
            if (!d.Equals(Dim.None))
                throw new InvalidOperationException($"{where}: requires dimensionless argument, got {d}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Divisible(int x, int n) => x % n == 0;

        public static bool TryRoot(in DimEval a, int n, out DimEval result)
        {
            if (n == 0) { result = default; return false; }
            if (TryRootDim(a.dim, n, out Dim d))
            {
                result = new DimEval(d, Math.Pow(a.scale, 1.0 / n));
                return true;
            }
            result = default;
            return false;
        }

        static bool TryRootDim(in Dim d, int n, out Dim r)
        {
            if (Divisible(d.l, n) &&
                Divisible(d.m, n) &&
                Divisible(d.t, n) &&
                Divisible(d.i, n) &&
                Divisible(d.temp, n) &&
                Divisible(d.n, n) &&
                Divisible(d.j, n))
            {
                r = new Dim(d.l / n, d.m / n, d.t / n, d.i / n, d.temp / n, d.n / n, d.j / n);
                return true;
            }
            r = default;
            return false;
        }

        /// <summary>
        /// Evaluates combined scale to SI base units.
        /// </summary>
        public static double ScaleToSI(in DimEval eval) => eval.scale;

        /// <summary>
        /// Scales current quantity to meters (length base).
        /// For example: cm^2 → 1e-4 m^2.
        /// </summary>
        public static double ScaleToMeters(in DimEval eval, UnitRegistry reg)
        {
            // Convert current scale (to SI) and dimensional powers into a pure length equivalent.
            // If dimension involves length (l ≠ 0), multiply by 1 m per power.
            double s = eval.scale;
            var d = eval.dim;

            if (d.l == 0) return s; // nothing to scale to meters

            // each length power stays in m — SI base already uses meters,
            // but if later we want e.g. feet, apply conversion
            var m = reg.Get("m");
            return s * Math.Pow(m.ScaleToSI, d.l);
        }

        /// <summary>
        /// Creates DimEval from a single unit symbol (handles prefixes etc).
        /// </summary>
        public static DimEval FromSymbol(string symbol, UnitRegistry reg)
        {
            var (u, factor) = reg.GetPrefixed(symbol);
            return new DimEval(u.Dim, u.ScaleToSI * factor);
        }
    }
}
