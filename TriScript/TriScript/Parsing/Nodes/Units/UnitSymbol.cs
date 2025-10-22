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
            throw new NotImplementedException();
        }
    }
}
