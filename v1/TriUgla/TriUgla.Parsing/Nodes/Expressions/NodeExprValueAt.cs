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

        protected override TuValue EvaluateInvariant(TuRuntime stack)
        {
            TuValue tuple = TupleExp.Evaluate(stack);
            if (tuple.type != EDataType.Tuple)
            {
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
