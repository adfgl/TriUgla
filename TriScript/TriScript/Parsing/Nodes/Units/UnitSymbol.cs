using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Units
{
    public sealed class UnitSymbol : UnitExpr
    {
        public string Name { get; }
        public UnitSymbol(Token id, string name) : base(id) { Name = name; }

        public override UnitEval Evaluate(UnitRegistry reg, DiagnosticBag diag)
        {
            if (Name == "1")
                return new UnitEval(1.0, Dimension.None, new Dictionary<string, int>());

            if (int.TryParse(Name, System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                diag.Report(ESeverity.Error,
                    "Bare integers (other than '1') are not allowed in unit expressions. Use '^' for exponents.",
                    Token.span);
                return new UnitEval(double.NaN, Dimension.None, new Dictionary<string, int>());
            }

            // Registry lookup
            if (reg.TryGet(Name, out var u))
            {
                var map = new Dictionary<string, int>(StringComparer.Ordinal) { [Name] = 1 };
                return new UnitEval(u.ScaleToMeter, u.Dim, map);
            }

            diag.Report(ESeverity.Error, $"Unknown unit '{Name}'.", Token.span);
            return new UnitEval(double.NaN, Dimension.None, new Dictionary<string, int>());
        }
    }

}
