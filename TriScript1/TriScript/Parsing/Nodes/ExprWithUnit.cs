using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprWithUnit : Expr
    {
        public ExprWithUnit(Token token, Expr inner) : base(token)
        {
            Inner = inner;
        }

        public Expr Inner { get; }

        public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
    }
}
