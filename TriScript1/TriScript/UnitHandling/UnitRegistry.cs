
namespace TriScript.UnitHandling
{
    public sealed class UnitRegistry
    {
        readonly Dictionary<string, Unit> _map = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> _alias = new(StringComparer.OrdinalIgnoreCase);

        // SI prefixes (longest-first; accept 'u' and 'µ' for micro)
        static readonly (string pfx, double factor)[] _si =
            {
            ("Y", 1e24), ("Z", 1e21), ("E", 1e18), ("P", 1e15), ("T", 1e12),
            ("G", 1e9),  ("M", 1e6),  ("k", 1e3),  ("h", 1e2),  ("da", 1e1),
            ("d", 1e-1), ("c", 1e-2), ("m", 1e-3), ("u", 1e-6), ("µ", 1e-6),
            ("n", 1e-9), ("p", 1e-12),("f", 1e-15),("a", 1e-18),("z", 1e-21),("y", 1e-24),
        };

        public UnitRegistry()
        {
            var L = Dim.Length; var M = Dim.Mass; var T = Dim.Time;
            var I = Dim.Current; var K = Dim.Temperature; var N = Dim.Substance; var J = Dim.Luminosity;

            Add(new Unit("m", 1.0, L), "meter", "metre");
            Add(new Unit("g", 1e-3, M), "gram");                   // prefixes go on g, not kg
            Add(new Unit("kg", 1.0, M, allowPrefixes: false));
            Add(new Unit("s", 1.0, T), "sec");
            Add(new Unit("A", 1.0, I), "amp", "ampere");
            Add(new Unit("K", 1.0, K), "kelvin");
            Add(new Unit("mol", 1.0, N));
            Add(new Unit("cd", 1.0, J));

            // Dimensionless angles
            Add(new Unit("rad", 1.0, Dim.None), "radian");
            Add(new Unit("deg", Math.PI / 180.0, Dim.None), "degree");

            // Affine temperatures (absolute)
            Add(new Unit("°C", 1.0, K, allowPrefixes: false, isAffine: true, offsetToSI: 273.15), "C", "degC");
            Add(new Unit("°F", 5.0 / 9.0, K, allowPrefixes: false, isAffine: true, offsetToSI: 255.37222222222223), "F", "degF");
            Add(new Unit("°R", 5.0 / 9.0, K, allowPrefixes: false), "Rankine"); // absolute, multiplicative

            // Common derived SI
            Add(new Unit("Hz", 1.0, Dim.Frequency));
            Add(new Unit("N", 1.0, Dim.Force));
            Add(new Unit("Pa", 1.0, Dim.Pressure));
            Add(new Unit("J", 1.0, Dim.Energy));
            Add(new Unit("W", 1.0, Dim.Power));
            Add(new Unit("C", 1.0, Dim.Charge));
            Add(new Unit("V", 1.0, Dim.Voltage));
            Add(new Unit("Ω", 1.0, Dim.Resistance), "Ohm", "ohm");
            Add(new Unit("S", 1.0, Dim.Conductance));
            Add(new Unit("F", 1.0, Dim.Capacitance));
            Add(new Unit("H", 1.0, Dim.Inductance));
            Add(new Unit("Wb", 1.0, Dim.MagneticFlux));
            Add(new Unit("T", 1.0, Dim.MagneticFluxDensity));

            // Photometry / fluid / chemistry
            Add(new Unit("lm", 1.0, Dim.LuminousFlux));
            Add(new Unit("lx", 1.0, Dim.Illuminance));
            Add(new Unit("L", 1e-3, Dim.Volume), "liter", "litre"); // 1 L = 1e-3 m^3
            Add(new Unit("bar", 1e5, Dim.Pressure, allowPrefixes: false));
            Add(new Unit("atm", 101_325.0, Dim.Pressure, allowPrefixes: false));
            Add(new Unit("mmHg", 133.32236842105263, Dim.Pressure, allowPrefixes: false));
            Add(new Unit("M", 1000.0, Dim.MolarConcentration, allowPrefixes: false), "mol/L");

            // Non-SI length
            Add(new Unit("in", 0.0254, L), "inch");
            Add(new Unit("ft", 0.3048, L), "foot");
            Add(new Unit("yd", 0.9144, L), "yard");
            Add(new Unit("mi", 1609.344, L), "mile");

            // Convenience (also covered by prefixes)
            Add(new Unit("mm", 1e-3, L));
            Add(new Unit("cm", 1e-2, L));
            Add(new Unit("km", 1e3, L));
        }

        // ---------------- Basic registry ops ----------------

        public void Add(Unit u, params string[] aliases)
        {
            _map[u.Symbol] = u;
            foreach (var a in aliases) _alias[a] = u.Symbol;
        }

        public bool TryGet(string sym, out Unit u) => _map.TryGetValue(Canonical(sym), out u);

        public Unit Get(string sym)
            => TryGet(sym, out var u) ? u : throw new ArgumentException($"Unknown unit '{sym}'.");

        public string Canonical(string sym)
            => _alias.TryGetValue(sym, out var s) ? s : sym;

        // ---------------- Integration with DimEval ----------------

        /// <summary>
        /// Public access for DimEval.FromSymbol: resolves symbol + optional SI prefix.
        /// </summary>
        public (Unit unit, double prefixFactor) GetPrefixed(string raw) => ResolvePrefixed(raw);

        /// <summary>
        /// Build a DimEval from a single symbol. For absolute temps (°C/°F),
        /// set isDifference=true only for temperature differences (ΔT).
        /// </summary>
        public DimEval EvalSymbol(string symbol, bool isDifference = false)
        {
            var (u, pf) = ResolvePrefixed(symbol);
            if (u.IsAffine && !isDifference)
                throw new InvalidOperationException($"'{symbol}' is affine; absolute conversion needs offset.");
            return new DimEval(u.Dim, u.ScaleToSI * pf);
        }

        // ---------------- Factors / conversion ----------------

        /// <summary>
        /// Pure multiplicative factor from symbol to SI (handles prefixes).
        /// For ΔT use isDifference=true; absolute °C/°F throws.
        /// </summary>
        public double FactorToSI(string symbol, bool isDifference = false)
        {
            var (u, pfxFactor) = ResolvePrefixed(symbol);
            if (u.IsAffine && !isDifference)
                throw new InvalidOperationException($"'{symbol}' is affine; absolute conversion needs offset.");
            return u.ScaleToSI * pfxFactor;
        }

        /// <summary>
        /// Pure factor between two symbols (handles prefixes).
        /// For ΔT use isDifference=true; absolute °C/°F throws.
        /// Optionally checks dimensions.
        /// </summary>
        public double FactorBetween(string from, string to, bool isDifference = false, bool checkDimensions = false)
        {
            var (uf, pf) = ResolvePrefixed(from);
            var (ut, pt) = ResolvePrefixed(to);

            if (checkDimensions && !uf.Dim.Equals(ut.Dim))
                throw new InvalidOperationException($"Incompatible dimensions: {uf.Dim} vs {ut.Dim}");

            if (isDifference || (!uf.IsAffine && !ut.IsAffine))
                return (uf.ScaleToSI * pf) / (ut.ScaleToSI * pt);

            throw new InvalidOperationException($"Absolute temperature conversion '{from}'→'{to}' needs offset; factor alone is undefined.");
        }

        /// <summary>
        /// Numeric conversion, including proper handling for absolute temperatures.
        /// Set isDifference=true for temperature differences (ΔT).
        /// </summary>
        public double Convert(double value, string from, string to, bool isDifference = false)
        {
            var (uf, pf) = ResolvePrefixed(from);
            var (ut, pt) = ResolvePrefixed(to);

            if (!uf.Dim.Equals(ut.Dim))
                throw new InvalidOperationException($"Incompatible dimensions: {uf.Dim} vs {ut.Dim}");

            bool isTemp = uf.Dim.Equals(Dim.Temperature);

            if (isDifference || !isTemp)
                return value * (uf.ScaleToSI * pf) / (ut.ScaleToSI * pt);

            // Absolute temperature path: apply offsets
            double si = value * (uf.ScaleToSI * pf) + (uf.IsAffine ? uf.OffsetToSI : 0.0);
            return (si - (ut.IsAffine ? ut.OffsetToSI : 0.0)) / (ut.ScaleToSI * pt);
        }

        (Unit unit, double prefixFactor) ResolvePrefixed(string raw)
        {
            // 1) Alias
            string sym = Canonical(raw);

            // 2) Direct symbol
            if (_map.TryGetValue(sym, out var u))
                return (u, 1.0);

            // 3) SI prefixes (longest-first)
            foreach (var (pfx, factor) in _si)
            {
                if (sym.StartsWith(pfx, StringComparison.Ordinal))
                {
                    string baseSym = Canonical(sym.Substring(pfx.Length));
                    if (_map.TryGetValue(baseSym, out var baseUnit)
                        && baseUnit.AllowPrefixes
                        && !baseUnit.IsAffine)
                    {
                        return (baseUnit, factor);
                    }
                }
            }

            // 4) Fail
            throw new ArgumentException($"Unknown unit '{raw}'.");
        }
    }
}
