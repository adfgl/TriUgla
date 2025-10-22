using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriScript.Data.Units
{
    public sealed class UnitRegistry
    {
        private readonly Dictionary<string, Unit> _map = new(StringComparer.Ordinal);

        public UnitRegistry()
        {
            var L = Dimension.Length;
            Add(new Unit("mm", 0.001, L));
            Add(new Unit("cm", 0.01, L));
            Add(new Unit("m", 1.0, L));
            Add(new Unit("km", 1000, L));
            Add(new Unit("in", 0.0254, L));
            Add(new Unit("ft", 0.3048, L));
            Add(new Unit("yd", 0.9144, L));
            Add(new Unit("mi", 1609.344, L));
        }

        public void Add(Unit u) => _map[u.Symbol] = u;
        public bool TryGet(string sym, out Unit u) => _map.TryGetValue(sym, out u);
        public Unit Get(string sym) => _map.TryGetValue(sym, out var u)
            ? u : throw new ArgumentException($"Unknown unit '{sym}'.");
    }
}
