using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprNumeric : Expr
    {
        readonly Value _value;

        public ExprNumeric(Token token, Value value) : base(token)
        {
            _value = value;
        }


        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            return _value;
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return _value.type;
        }

        public override bool EvaluateToSI(Source src, ScopeStack stack, ObjHeap heap, DiagnosticBag diagnostics, out double si, out Dimension dim)
        {
            si = _value.AsDouble();
            dim = Dimension.None;
            return true;
        }
    }
}
