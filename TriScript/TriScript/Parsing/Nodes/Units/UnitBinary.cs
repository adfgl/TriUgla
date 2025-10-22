using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Units
{
    public sealed class UnitBinary : UnitExpr
    {
        public UnitExpr Left { get; }
        public Token Op { get; }
        public UnitExpr Right { get; }

        public UnitBinary(UnitExpr left, Token op, UnitExpr right) : base(op)
            => (Left, Op, Right) = (left, op, right);

        public override UnitEval Evaluate(UnitRegistry reg, DiagnosticBag diag)
        {
            var L = Left.Evaluate(reg, diag);
            var R = Right.Evaluate(reg, diag);

            switch (Op.type)
            {
                case ETokenType.Star:
                    return new UnitEval(L.ScaleToMeter * R.ScaleToMeter, L.Dim + R.Dim);

                case ETokenType.Slash:
                    return new UnitEval(L.ScaleToMeter / R.ScaleToMeter, L.Dim - R.Dim);

                case ETokenType.Pow:
                    if (Right is UnitSymbol rs && int.TryParse(rs.Name, out int n))
                        return new UnitEval(Math.Pow(L.ScaleToMeter, n), L.Dim.Pow(n));

                    diag.Report(ESeverity.Error, "Exponent in unit must be an integer (e.g., ^-2).", Op.span);
                    return new UnitEval(double.NaN, L.Dim);
            }

            diag.Report(ESeverity.Error, $"Unsupported unit operator '{Op.type}'.", Op.span);
            return new UnitEval(double.NaN, Dimension.None);
        }
    }
}
