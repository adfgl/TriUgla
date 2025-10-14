using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeValueAt : NodeBase
    {
        public NodeValueAt(Token token, NodeBase tuple, NodeBase index) : base(token)
        {
            TupleExp = tuple;
            IndexExp = index;
        }

        public NodeBase TupleExp { get; }
        public NodeBase IndexExp { get; }
        public int Index { get; private set; }
        public TuTuple? Tuple { get; private set; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue tuple = TupleExp.Evaluate(stack);
            if (tuple.type != EDataType.Tuple)
            {
                if (TupleExp is NodeIdentifier id)
                {
                    throw new CompiletimeException(
                        $"Cannot index variable '{id.Name}': expected a tuple, but it is of type '{tuple.type}'.",
                        Token);
                }

                throw new RuntimeException(
                    $"Cannot index expression of type '{tuple.type}'. Only tuples ({{...}}) support indexing.",
                    Token);
            }

            TuTuple tpl = tuple.AsTuple()!;

            TuValue index = IndexExp.Evaluate(stack);
            if (index.type != EDataType.Numeric)
            {
                string msg = $"Tuple index must be numeric, but expression evaluated to '{index.type}'.";
                if (IndexExp is NodeLiteralBase)
                {
                    throw new CompiletimeException(msg, IndexExp.Token);
                }
                else
                {
                    throw new RuntimeException(msg, IndexExp.Token);
                }
            }

            double idxNum = index.AsNumeric();
            if (idxNum % 1 != 0)
            {
                throw new RuntimeException(
                    $"Tuple index must be an integer value, but got {idxNum}.",
                    IndexExp.Token);
            }
                
            int i = (int)idxNum;

            List<double> t = tpl.Values;
            if (i < 0 || i >= t.Count)
            {
                throw new RuntimeException(
                    $"Tuple index {i} is out of range (valid range: 0–{t.Count - 1}).",
                    IndexExp.Token);
            }

            Index = i;
            Tuple = tpl;
            return new TuValue(t[i]);
        }
    }
}
