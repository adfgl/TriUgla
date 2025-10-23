using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprIdentifier : Expr
    {
        public ExprIdentifier(Token token) : base(token)
        {
        }

        public override T Accept<T>(IExprVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
