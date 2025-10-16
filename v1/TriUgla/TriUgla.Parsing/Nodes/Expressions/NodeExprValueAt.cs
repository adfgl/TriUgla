using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprValueAt : NodeExprBase
    {
        public NodeExprValueAt(Token token, NodeExprBase tuple, NodeExprBase index) : base(token)
        {
            TupleExp = tuple;
            IndexExp = index;
        }

        public NodeExprBase TupleExp { get; }
        public NodeExprBase IndexExp { get; }
        public int Index { get; private set; }
        public TuTuple? Tuple { get; private set; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            TuValue tuple = TupleExp.Evaluate(rt);
            if (tuple.type != EDataType.Tuple)
            {
                throw new RunTimeException(
                    $"Cannot index expression of type '{tuple.type}'. Only tuples ({{...}}) support indexing.",
                    Token);
            }

            TuTuple tpl = tuple.AsTuple()!;

            TuValue index = IndexExp.Evaluate(rt);
            if (index.type != EDataType.Integer)
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

                
            int i = (int)index.AsNumeric();
            if (i < 0 || i >= tpl.Count)
            {
                throw new RunTimeException(
                    $"Tuple index {i} is out of range (valid range: 0–{tpl.Count - 1}).",
                    IndexExp.Token);
            }

            Index = i;
            Tuple = tpl;
            return tpl[i];
        }
    }
}
