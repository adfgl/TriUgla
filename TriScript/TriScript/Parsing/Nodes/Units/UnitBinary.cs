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
            switch (Op.type)
            {
                case ETokenType.Pow:
                    {
                        // Do NOT evaluate Right for '^' — it’s a UnitInt
                        if (Right is UnitInt ui)
                        {
                            var L = Left.Evaluate(reg, diag);
                            return L.Pow(ui.Value);
                        }
                        // Back-compat fallback if something else slipped in
                        if (Right is UnitSymbol rs && int.TryParse(rs.Name, out int n))
                        {
                            var L = Left.Evaluate(reg, diag);
                            return L.Pow(n);
                        }
                        diag.Report(ESeverity.Error, "Exponent in unit must be an integer (e.g., ^-2).", Op.span);
                        return new UnitEval(double.NaN, Dimension.None);
                    }

                case ETokenType.Star:
                    {
                        var L = Left.Evaluate(reg, diag);
                        var R = Right.Evaluate(reg, diag);
                        return L.Mul(R);
                    }

                case ETokenType.Slash:
                    {
                        var L = Left.Evaluate(reg, diag);
                        var R = Right.Evaluate(reg, diag);
                        return L.Div(R);
                    }
            }

            diag.Report(ESeverity.Error, $"Unsupported unit operator '{Op.type}'.", Op.span);
            return new UnitEval(double.NaN, Dimension.None);
        }
    }


}
