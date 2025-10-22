using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes.Expressions.Literals;

namespace TriScript.Parsing.Nodes.Expressions
{
    public sealed class ExprWithUnit : Expr
    {
        public Expr Inner { get; }
        public UnitEval Units { get; }

        public ExprWithUnit(Expr inner, UnitEval units) : base(inner.Token)
        {
            Inner = inner; Units = units;
        }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            var tmp = new DiagnosticBag();
            if (!EvaluateToSI(source, stack, heap, tmp, out double si, out _))
                return new Value(double.NaN);

            // SI → requested display unit
            double display = si / Units.ScaleToMeter;
            return new Value(display);
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
            => EDataType.Real;

        public override bool EvaluateToSI(Source source, ScopeStack stack, ObjHeap heap,
                                          DiagnosticBag diagnostics, out double si, out Dimension dim)
        {
            // Reduce inner to SI
            if (!Inner.EvaluateToSI(source, stack, heap, diagnostics, out double innerSI, out Dimension innerDim))
            { si = double.NaN; dim = Dimension.None; return false; }

            if (innerDim.Equals(Dimension.None))
            {
                // Plain number being *interpreted as* Units → scale to SI
                si = innerSI * Units.ScaleToMeter;
                dim = Units.Dim;
                return true;
            }

            if (!innerDim.Equals(Units.Dim))
            {
                diagnostics.Report(
                    ESeverity.Warning,
                    $"Unit cast dimension mismatch: {innerDim} cannot be cast to {Units.Dim}. Treating result as dimensionless.",
                    Token.span);

                // Keep numeric (already SI), but drop dimension
                si = innerSI;
                dim = Dimension.None;
                return true;
            }

            // Matching dimensions: inner is already SI of that dimension;
            // EvaluateToSI returns SI unchanged and the same dimension.
            si = innerSI;
            dim = innerDim;
            return true;
        }

        public override UnitEval? EvaluateToUnit(Source s, ScopeStack st, ObjHeap h) => Units;
    }
}
