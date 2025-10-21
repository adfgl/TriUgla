using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class ExprBinary : Expr
    {
        public ExprBinary(Expr left, Token op, Expr right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public Expr Left { get; }
        public Token Operator { get; }  
        public Expr Right { get; }

        public override Value Evaluate(Source source, ScopeStack stack, ObjHeap heap)
        {
            throw new NotImplementedException();
        }
    }
}
