using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Units
{
    public sealed class UnitSymbol : UnitExpr
    {
        public UnitSymbol(Token id, string name) : base(id)
        {
            this.Name = name;
        }

        public string Name { get; }

        public override UnitEval Evaluate(UnitRegistry reg, DiagnosticBag diag)
        {
            if (int.TryParse(Name, System.Globalization.NumberStyles.Integer,
                          System.Globalization.CultureInfo.InvariantCulture, out int n))
            {
                // integer "unit" evaluates to dimensionless numeric constant
                return new UnitEval(n, Dimension.None);
            }

            // Case 2: unit symbol lookup
            if (reg.TryGet(Name, out var u))
            {
                return new UnitEval(u.ScaleToMeter, u.Dim);
            }

            // Case 3: unknown symbol → diagnostic + fallback
            diag.Report(ESeverity.Error, $"Unknown unit '{Name}'.", Token.span);
            return new UnitEval(double.NaN, Dimension.None);
        }
    }
}
