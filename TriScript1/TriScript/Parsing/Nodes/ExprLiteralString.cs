using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class ExprLiteralString : ExprLiteralBase
    {
        public ExprLiteralString(Token token, Value value) : base(token, value)
        {
        }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
