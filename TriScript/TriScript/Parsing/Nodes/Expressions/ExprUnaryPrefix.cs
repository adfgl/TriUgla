using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprUnaryPrefix : Expr
    {
        public ExprUnaryPrefix(Token op, Expr expr) : base(op)
        {
            Operator = op;
            Expr = expr;
        }

        public Token Operator { get; }
        public Expr Expr { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            throw new NotImplementedException();
        }

        public override EDataType PreviewType(Source source, ScopeStack stack, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        public override bool EvaluateToSI(Source src, ScopeStack stack, ObjHeap heap, DiagnosticBag diagnostics, out double si, out Dimension dim)
        {
            throw new NotImplementedException();
        }
    }
}
