using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitEngine
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
            Add(new Unit("km", 1000.0, L));
        }

        private void Add(Unit u) => _map[u.Symbol] = u;

        public bool TryGet(string symbol, out Unit unit) => _map.TryGetValue(symbol, out unit);

        public Unit Get(string symbol)
            => _map.TryGetValue(symbol, out var u)
               ? u : throw new ArgumentException($"Unknown unit '{symbol}'.");

        /// <summary>
        /// Parses units like "m", "cm^3", "ft^2"
        /// </summary>
        public Unit ParseUnitWithPower(string expr)
        {
            expr = expr.Trim();
            int power = 1;

            // handle power suffix like ^2 or ^3
            int caret = expr.IndexOf('^');
            if (caret >= 0)
            {
                string sym = expr[..caret];
                string powStr = expr[(caret + 1)..];
                power = int.Parse(powStr, CultureInfo.InvariantCulture);
                expr = sym;
            }

            var baseUnit = Get(expr);
            return new Unit(expr + (power == 1 ? "" : $"^{power}"),
                            Math.Pow(baseUnit.ScaleToMeter, power),
                            baseUnit.Dim.Pow(power));
        }

        /// <summary>
        /// Converts a value between compatible units (e.g., m³ → cm³)
        /// </summary>
        public double Convert(double value, string fromExpr, string toExpr)
        {
            var from = ParseUnitWithPower(fromExpr);
            var to = ParseUnitWithPower(toExpr);

            if (!from.Dim.Equals(to.Dim))
                throw new InvalidOperationException($"Incompatible dimensions: {from.Dim} vs {to.Dim}");

            double baseValue = value * from.ScaleToMeter;
            return baseValue / to.ScaleToMeter;
        }
    }
}
