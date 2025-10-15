using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeExprValueAt : NodeBase
    {
        public NodeExprValueAt(Token token, NodeBase tuple, NodeBase index) : base(token)
        {
            TupleExp = tuple;
            IndexExp = index;
        }

        public NodeBase TupleExp { get; }
        public NodeBase IndexExp { get; }
        public int Index { get; private set; }
        public TuTuple? Tuple { get; private set; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            TuValue tuple = TupleExp.Evaluate(stack);
            if (tuple.type != EDataType.Tuple)
            {
                if (TupleExp is NodeExprIdentifier id)
                {
                    throw new CompileTimeException(
                        $"Cannot index variable '{id.Name}': expected a tuple, but it is of type '{tuple.type}'.",
                        Token);
                }

                throw new RunTimeException(
                    $"Cannot index expression of type '{tuple.type}'. Only tuples ({{...}}) support indexing.",
                    Token);
            }

            TuTuple tpl = tuple.AsTuple()!;

            TuValue index = IndexExp.Evaluate(stack);
            if (index.type != EDataType.Numeric)
            {
                string msg = $"Tuple index must be numeric, but expression evaluated to '{index.type}'.";
                if (IndexExp is NodeExprLiteralBase)
                {
                    throw new CompileTimeException(msg, IndexExp.Token);
                }
                else
                {
                    throw new RunTimeException(msg, IndexExp.Token);
                }
            }

            double idxNum = index.AsNumeric();
            if (idxNum % 1 != 0)
            {
                throw new RunTimeException(
                    $"Tuple index must be an integer value, but got {idxNum}.",
                    IndexExp.Token);
            }
                
            int i = (int)idxNum;

            List<double> t = tpl.Values;
            if (i < 0 || i >= t.Count)
            {
                throw new RunTimeException(
                    $"Tuple index {i} is out of range (valid range: 0–{t.Count - 1}).",
                    IndexExp.Token);
            }

            Index = i;
            Tuple = tpl;
            Value = new TuValue(t[i]);
            return Value;
        }
    }
}
