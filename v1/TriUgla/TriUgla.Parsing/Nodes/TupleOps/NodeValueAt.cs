using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
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
                throw new Exception("Indexing requires a tuple");
            }

            TuTuple tpl = tuple.AsTuple()!;

            TuValue index = IndexExp.Evaluate(stack);
            if (index.type != EDataType.Numeric)
            {
                throw new Exception("Index must be numeric");
            }

            if (index.AsNumeric() % 1 != 0)
            {
                throw new Exception("Index must be integer");
            }
                
            int i = (int)index.AsNumeric();

            List<double> t = tpl.Values;
            if (i < 0 || i >= t.Count)
            {
                throw new Exception("Index out of range");
            }

            Index = i;
            Tuple = tpl;
            return new TuValue(t[i]);
        }
    }
}
