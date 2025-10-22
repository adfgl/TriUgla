using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class UnitExpr
    {
        protected UnitExpr(Token token) { Token = token; }
        public Token Token { get; }
        public abstract UnitEval Evaluate(UnitRegistry reg, DiagnosticBag diag);
    }
}
