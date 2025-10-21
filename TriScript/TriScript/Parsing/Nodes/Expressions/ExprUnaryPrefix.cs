using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprUnaryPrefix : Expr
    {
        public ExprUnaryPrefix(Token op, Expr expr)
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
    }
}
