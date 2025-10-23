
namespace TriScript.UnitHandling
{
    public static class UnitPretty
    {
        public static string ToCanonicalString(UnitNode node)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            Flatten(node, +1, map);

            var num = new List<(string sym, int pow)>();
            var den = new List<(string sym, int pow)>();

            foreach (var (sym, pow) in map)
            {
                if (pow > 0) num.Add((sym, pow));
                else if (pow < 0) den.Add((sym, -pow));
            }

            int Cmp((string sym, int pow) a, (string sym, int pow) b)
            {
                int c = Math.Abs(a.pow).CompareTo(Math.Abs(b.pow));
                return c != 0 ? c : StringComparer.Ordinal.Compare(a.sym, b.sym);
            }
            num.Sort(Cmp);
            den.Sort(Cmp);

            static string F((string sym, int pow) x) => x.pow == 1 ? x.sym : $"{x.sym}^{x.pow}";
            string ns = num.Count == 0 ? "1" : string.Join("·", num.Select(F));
            string ds = den.Count == 0 ? "" : string.Join("·", den.Select(F));
            return den.Count == 0 ? ns : $"{ns} / ({ds})";
        }

        static void Flatten(UnitNode n, int sign, Dictionary<string, int> map)
        {
            switch (n)
            {
                case UnitSym s:
                    Add(map, s.Symbol, sign);
                    break;

                case UnitPow p:
                    var tmp = new Dictionary<string, int>(StringComparer.Ordinal);
                    Flatten(p.Base, 1, tmp);
                    foreach (var kv in tmp) Add(map, kv.Key, kv.Value * p.Exponent * sign);
                    break;

                case UnitMul m:
                    foreach (var f in m.Factors) Flatten(f, sign, map);
                    break;

                case UnitDiv d:
                    Flatten(d.Numerator, sign, map);
                    Flatten(d.Denominator, -sign, map);
                    break;

                case UnitGroup g:
                    Flatten(g.Inner, sign, map);
                    break;
            }
        }

        static void Add(Dictionary<string, int> map, string sym, int delta)
        {
            if (delta == 0) return;
            map.TryGetValue(sym, out int cur);
            int next = cur + delta;
            if (next == 0) map.Remove(sym);
            else map[sym] = next;
        }
    }
}
