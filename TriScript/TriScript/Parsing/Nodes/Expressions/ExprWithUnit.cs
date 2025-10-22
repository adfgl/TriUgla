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
        { Inner = inner; Units = units; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            // Evaluate inner to numeric and convert to the requested unit's numeric value
            Value v = Inner.Evaluate(source, stack, heap);

            if (!v.type.IsNumeric()) return new Value(double.NaN);

            // If inner is an identifier with unit-bound storage, convert from its canonical SI:
            if (Inner is ExprIdentifier idExpr)
            {
                string id = source.GetString(idExpr.Token.span);
                if (stack.Current.TryGet(id, out Variable var) && var.Units is not null)
                {
                    if (!var.Units.Dim.Equals(Units.Dim))
                        return new Value(double.NaN);

                    // SI -> requested units
                    double converted = var.Units.SiValue / Units.ScaleToMeter;
                    return new Value(converted);
                }
            }

            // Otherwise: treat numeric as **value in requested unit**, return normalized numeric in that unit
            // (This is for cast-in-expression use, not binding. Assignment handles binding rules.)
            double num = v.AsDouble();
            return new Value(num); // for expression casts we return the numeric *in* requested unit
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
            => EDataType.Real;
    }
}
