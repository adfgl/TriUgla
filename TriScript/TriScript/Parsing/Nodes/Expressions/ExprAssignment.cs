using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public sealed class ExprAssignment : Expr
    {
        public ExprAssignment(Token identifier, Expr value)
            : base(identifier)
        {
            ValueExpr = value;
        }

        public Expr ValueExpr { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string name = source.GetString(Token.span);

            bool declared = !stack.Current.TryGet(name, out Variable var);
            if (declared)
            {
                var = new Variable(name);
                stack.Current.Declare(var);
            }

            // Try to reduce RHS to canonical SI + Dimension (using temporary diag)
            var tempDiag = new DiagnosticBag();
            if (ValueExpr.EvaluateToSI(source, stack, heap, tempDiag, out double rhsSI, out Dimension rhsDim))
            {
                // Determine preferred unit
                UnitEval? preferred = (ValueExpr is ExprWithUnit w) ? w.Units : (UnitEval?)null;
                if (!preferred.HasValue && !declared && var.Units is not null)
                    preferred = var.Units.Preferred;
                if (!preferred.HasValue)
                    preferred = new UnitEval(1.0, rhsDim);

                // If var already had units but dimensions differ, just continue (warn logged inside subnodes)
                if (!declared && var.Units is not null && !var.Units.Dim.Equals(rhsDim))
                {
                    // Downgrade to dimensionless for safety
                    rhsDim = Dimension.None;
                    preferred = new UnitEval(1.0, rhsDim);
                }

                // Bind/update units
                var.Units ??= new UnitBinding();
                var.Units.Dim = rhsDim;
                var.Units.SiValue = rhsSI;
                var.Units.Preferred = preferred.Value;

                // Display numeric = SI / preferred scale (if compatible)
                double display = rhsSI;
                if (preferred.Value.Dim.Equals(rhsDim) && preferred.Value.ScaleToMeter != 0.0)
                    display = rhsSI / preferred.Value.ScaleToMeter;

                var.Value = new Value(display);
                return var.Value;
            }

            // RHS did not produce SI — fallback to raw
            Value raw = ValueExpr.Evaluate(source, stack, heap);

            if (declared)
            {
                var.Units = null;
                var.Value = raw;
                return var.Value;
            }

            // Reassign without unit info but existing unit and numeric RHS
            if (var.Units != null && raw.type.IsNumeric())
            {
                double num = raw.AsDouble();
                var.Units.SiValue = num * var.Units.Preferred.ScaleToMeter;
                var.Value = new Value(num);
                return var.Value;
            }

            // Otherwise plain scalar overwrite
            var.Units = null;
            var.Value = raw;
            return var.Value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
            => ValueExpr.PreviewType(source, stack, diagnostics);

        public override bool EvaluateToSI(Source source, ScopeStack stack, ObjHeap heap,
                                          DiagnosticBag diagnostics, out double si, out Dimension dim)
        {
            // Try RHS first
            if (ValueExpr.EvaluateToSI(source, stack, heap, diagnostics, out si, out dim))
                return true;

            // Fallback: if variable exists and has units, interpret numeric RHS in Preferred
            string name = source.GetString(Token.span);
            stack.Current.TryGet(name, out Variable var);

            var v = ValueExpr.Evaluate(source, stack, heap);
            if (var is not null && var.Units is not null && v.type.IsNumeric())
            {
                si = v.AsDouble() * var.Units.Preferred.ScaleToMeter;
                dim = var.Units.Dim;
                return true;
            }

            // Last resort: dimensionless numeric
            if (v.type.IsNumeric())
            {
                si = v.AsDouble();
                dim = Dimension.None;
                return true;
            }

            si = double.NaN;
            dim = Dimension.None;
            return false;
        }
    }



}
