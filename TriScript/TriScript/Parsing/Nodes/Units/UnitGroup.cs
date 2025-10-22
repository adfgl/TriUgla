using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Units
{
    public sealed class UnitGroup : UnitExpr
    {
        public UnitExpr Inner { get; }
        public UnitGroup(Token open, UnitExpr inner, Token close) : base(open) { Inner = inner; }
        public override UnitEval Evaluate(UnitRegistry reg, DiagnosticBag diag) => Inner.Evaluate(reg, diag);
    }
}
