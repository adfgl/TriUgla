using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriScript.Data.Units
{
    public readonly struct UnitEval
    {
        public readonly double ScaleToMeter;                 // numeric scale → SI
        public readonly Dimension Dim;                       // physical dimension (e.g., L^k)
        public readonly IReadOnlyDictionary<string, int> Exponents; // symbol→power (cm→2, m→-1)

        public UnitEval(double scaleToMeter, Dimension dim)
            : this(scaleToMeter, dim, Empty) { }

        public UnitEval(double scaleToMeter, Dimension dim, IReadOnlyDictionary<string, int> exponents)
        {
            ScaleToMeter = scaleToMeter;
            Dim = dim;
            Exponents = exponents;
        }

        public bool IsValid => !double.IsNaN(ScaleToMeter) && !double.IsInfinity(ScaleToMeter);

        public UnitEval Mul(UnitEval other)
            => new UnitEval(ScaleToMeter * other.ScaleToMeter, Dim + other.Dim, Merge(Exponents, other.Exponents, +1));

        public UnitEval Div(UnitEval other)
            => new UnitEval(ScaleToMeter / other.ScaleToMeter, Dim - other.Dim, Merge(Exponents, other.Exponents, -1));

        public UnitEval Pow(int n)
            => new UnitEval(Math.Pow(ScaleToMeter, n), Dim.Pow(n), Scale(Exponents, n));

        public override string ToString()
        {
            BuildSymbol(Exponents, out string num, out string den);
            if (num.Length == 0 && den.Length == 0) return "-"; 
            if (den.Length == 0) return num;
            if (num.Length == 0) return $"1/{den}";
            return $"{num}/{den}";
        }

        // ---- helpers ----
        private static readonly IReadOnlyDictionary<string, int> Empty = new Dictionary<string, int>();

        private static IReadOnlyDictionary<string, int> Merge(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b, int signB)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in a) map[kv.Key] = kv.Value;
            foreach (var kv in b)
            {
                map.TryGetValue(kv.Key, out int cur);
                int now = cur + signB * kv.Value;
                if (now == 0) map.Remove(kv.Key);
                else map[kv.Key] = now;
            }
            return map;
        }

        private static IReadOnlyDictionary<string, int> Scale(IReadOnlyDictionary<string, int> a, int n)
        {
            if (n == 1) return a;
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in a) map[kv.Key] = kv.Value * n;
            return map;
        }

        private static void BuildSymbol(IReadOnlyDictionary<string, int> exp, out string numerator, out string denominator)
        {
            var pos = new List<(string s, int p)>(); var neg = new List<(string s, int p)>();
            foreach (var kv in exp) { if (kv.Value > 0) pos.Add((kv.Key, kv.Value)); else if (kv.Value < 0) neg.Add((kv.Key, -kv.Value)); }
            pos.Sort((a, b) => string.Compare(a.s, b.s, StringComparison.Ordinal));
            neg.Sort((a, b) => string.Compare(a.s, b.s, StringComparison.Ordinal));
            numerator = Join(pos); denominator = Join(neg);

            static string Join(List<(string s, int p)> list)
            {
                if (list.Count == 0) return "";
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append('*');
                    sb.Append(list[i].s);
                    if (list[i].p != 1) { sb.Append('^'); sb.Append(list[i].p); }
                }
                return sb.ToString();
            }
        }
    }


}
