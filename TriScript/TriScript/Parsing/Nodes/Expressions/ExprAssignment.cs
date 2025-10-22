
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes.Expressions.Literals;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public sealed class ExprAssignment : Expr
    {
        public ExprAssignment(Token identifier, Expr value) : base(identifier)
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

            // Evaluate RHS
            Value value = ValueExpr.Evaluate(source, stack, heap);

            // Detect a unit-cast on RHS (ExprWithUnit) so we can bind/convert storage properly
            UnitEval? rhsCastUnits = TryExtractUnitsFromRHS(ValueExpr, out var rhsInner);

            // --- No unit cast on RHS ---
            if (!rhsCastUnits.HasValue)
            {
                if (declared)
                {
                    var.Units = null;
                    var.Value = value;
                    return var.Value;
                }

                // re-assignment without [units]
                if (var.Units != null && value.type.IsNumeric())
                {
                    double num = value.AsDouble();
                    double si = num * var.Units.Preferred.ScaleToMeter;
                    var.Units.SiValue = si;               // update canonical
                    var.Value = new Value(num);           // display stays in preferred
                    return var.Value;
                }

                var.Units = null;
                var.Value = value;
                return var.Value;
            }

            // --- There IS a unit cast on RHS ---
            UnitEval ue = rhsCastUnits.Value;

            if (declared)
            {
                if (value.type.IsNumeric())
                {
                    double num = value.AsDouble();
                    double si = num * ue.ScaleToMeter;

                    var.Units ??= new UnitBinding();
                    var.Units.Dim = ue.Dim;
                    var.Units.SiValue = si;
                    var.Units.Preferred = ue;

                    var.Value = new Value(num); // display in annotated unit
                    return var.Value;
                }

                // non-numeric w/ units → fallback scalar
                var.Units = null;
                var.Value = value;
                return var.Value;
            }

            // Reassignment (var already exists)
            // Conversion form: a = a [mm^2]  => ValueExpr is ExprWithUnit(Inner=ExprIdentifier("a"))
            if (rhsInner is ExprIdentifier idRef)
            {
                string rhsName = source.GetString(idRef.Token.span);
                if (!stack.Current.TryGet(rhsName, out Variable src) || src.Units is null)
                {
                    var.Units = null;
                    var.Value = value;
                    return var.Value;
                }

                // dimension checks
                if (!src.Units.Dim.Equals(ue.Dim) || (var.Units != null && !var.Units.Dim.Equals(ue.Dim)))
                {
                    var.Value = new Value(double.NaN);
                    return var.Value;
                }

                // SI -> requested display
                double converted = src.Units.SiValue / ue.ScaleToMeter;

                var.Units ??= new UnitBinding();
                var.Units.Dim = src.Units.Dim;
                var.Units.SiValue = src.Units.SiValue; // keep canonical SI
                var.Units.Preferred = ue;              // new preferred unit

                var.Value = new Value(converted);
                return var.Value;
            }

            // Numeric reassignment with [units]
            if (value.type.IsNumeric())
            {
                double num = value.AsDouble();

                if (var.Units != null && !var.Units.Dim.Equals(ue.Dim))
                {
                    var.Value = new Value(double.NaN);
                    return var.Value;
                }

                double si = num * ue.ScaleToMeter;

                var.Units ??= new UnitBinding();
                var.Units.Dim = ue.Dim;
                var.Units.SiValue = si;
                var.Units.Preferred = ue;

                var.Value = new Value(num);
                return var.Value;
            }

            // non-numeric with cast → plain scalar
            var.Units = null;
            var.Value = value;
            return var.Value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return ValueExpr.PreviewType(source, stack, diagnostics);
        }

        private static UnitEval? TryExtractUnitsFromRHS(Expr expr, out Expr inner)
        {
            if (expr is ExprWithUnit w)
            {
                inner = w.Inner;
                return w.Units;
            }
            inner = expr;
            return null;
        }
    }

}
