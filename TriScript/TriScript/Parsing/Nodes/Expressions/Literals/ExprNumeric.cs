using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions.Literals
{
    public sealed class ExprNumeric : Expr
    {
        public ExprNumeric(Token token, bool isInteger) : base(token)
        {
            IsInteger = isInteger;
        }

        public bool IsInteger { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            double value = double.Parse(source.GetString(Token.span));
            return new Value(IsInteger ? (int)value : value);   
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            return IsInteger ? EDataType.Integer : EDataType.Numeric;
        }
    }
}
