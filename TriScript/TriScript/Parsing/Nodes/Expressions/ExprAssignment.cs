
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes.Expressions.Literals;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprAssignment : Expr
    {
        public ExprAssignment(Token target, Expr value, UnitExpr? units = null, UnitEval? unitEval = null) : base(target)
        {
            Value = value;
            UnitsAst = units;  
            UnitsEval = unitEval;
        }

        public Expr Value { get; }
        public UnitExpr? UnitsAst { get; }
        public UnitEval? UnitsEval { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            string name = source.GetString(Token.span);

            bool declared = !stack.Current.TryGet(name, out Variable var);
            if (declared)
            {
                var = new Variable(name);
                stack.Current.Declare(var);
            }

            // Evaluate RHS expression
            Value value = Value.Evaluate(source, stack, heap);

            // --- CASE 1: No unit annotation ---
            if (!UnitsEval.HasValue)
            {
                if (declared)
                {
                    // first declaration, no units → just assign
                    var.Units = null;
                    var.Value = value;
                    return var.Value;
                }

                // re-assignment without [units]
                if (var.Units != null && value.type.IsNumeric())
                {
                    double num = value.AsDouble();
                    double si = num * var.Units.Preferred.ScaleToMeter;
                    var.Units.SiValue = si;
                    var.Value = new Value(num);
                    return var.Value;
                }

                // otherwise, just overwrite as plain scalar
                var.Units = null;
                var.Value = value;
                return var.Value;
            }

            // --- CASE 2: There is a [ ... ] unit annotation ---
            UnitEval ue = UnitsEval.Value;

            // --- Declared variable ---
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

                    var.Value = new Value(num);
                    return var.Value;
                }

                // non-numeric RHS with units → store as scalar, drop units
                var.Units = null;
                var.Value = value;
                return var.Value;
            }

            // --- Re-assignment (variable already exists) ---
            // Handle special conversion case: a = a [mm^2]
            if (Value is ExprIdentifier idRef)
            {
                string rhsName = source.GetString(idRef.Token.span);

                if (!stack.Current.TryGet(rhsName, out Variable src) || src.Units is null)
                {
                    // no unit-bound variable to convert from
                    var.Units = null;
                    var.Value = value;
                    return var.Value;
                }

                // dimension check
                if (!src.Units.Dim.Equals(ue.Dim) || (var.Units != null && !var.Units.Dim.Equals(ue.Dim)))
                {
                    var.Value = new Value(double.NaN);
                    return var.Value;
                }

                // conversion: canonical SI unchanged, convert to requested display
                double converted = src.Units.SiValue / ue.ScaleToMeter;

                var.Units ??= new UnitBinding();
                var.Units.Dim = src.Units.Dim;
                var.Units.SiValue = src.Units.SiValue;
                var.Units.Preferred = ue;

                var.Value = new Value(converted);
                return var.Value;
            }

            // --- Regular numeric reassignment with [units] ---
            if (value.type.IsNumeric())
            {
                double num = value.AsDouble();

                // check dimension match
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

            // non-numeric reassignment with units → plain overwrite
            var.Units = null;
            var.Value = value;
            return var.Value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return Value.PreviewType(source, stack, diagnostics);
        }
    }
}
