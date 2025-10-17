using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public class NodeExprCast : NodeExprBase
    {
        public NodeExprCast(Token token, NodeExprBase expr) : base(token)
        {
            Expr = expr;
        }

        public NodeExprBase Expr { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            EDataType dt = Token.type switch
            {
                ETokenType.Integer => EDataType.Integer,
                ETokenType.Float => EDataType.Float,
                ETokenType.String => EDataType.String,
                ETokenType.List => EDataType.List,
                _ => throw new Exception(),
            };

            TuValue value = Expr.Evaluate(rt);
            if (value.type == dt) return value;
            if (dt == EDataType.String) return new TuValue(value.AsString());    

            if (dt == EDataType.Integer && value.type == EDataType.Float)
            {
                return new TuValue((int)value.AsNumeric());
            }

            if (dt == EDataType.Float && value.type == EDataType.Integer)
            {
                return new TuValue(value.AsNumeric());
            }

            throw new Exception();
        }
    }
}
