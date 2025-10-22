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
    public sealed class UnitInt : UnitExpr
    {
        public int Value { get; }
        public UnitInt(Token token, int value) : base(token) => Value = value;

        // Should never be evaluated outside '^' context; return a sentinel if it happens.
        public override UnitEval Evaluate(UnitRegistry reg, DiagnosticBag diag)
            => new UnitEval(double.NaN, Dimension.None);
    }
}
