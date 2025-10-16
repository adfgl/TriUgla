using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprTuple : NodeExprLiteralBase
    {
        TuValue _value = TuValue.Nothing;

        public NodeExprTuple(Token open, IEnumerable<NodeExprBase> args, Token close) : base(open)
        {
            Args = args.ToArray();
            Close = close;
        }

        public Token Open => Token;
        public IReadOnlyList<NodeExprBase> Args { get; }
        public Token Close { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            if (_value.type != EDataType.Nothing)
            {
                return _value;
            }

            bool allCompileTimeKnown = true;
            TuTuple values = new TuTuple(Args.Count);
            for (int i = 0; i < Args.Count; i++)
            {
                NodeBase item = Args[i];
                if (allCompileTimeKnown && item is not NodeExprLiteralBase)
                {
                    allCompileTimeKnown = false;
                }

                TuValue v = item.Evaluate(rt);
                if (!TuValue.Compatible(values.Type, v.type))
                {
                    throw new RunTimeException(
                        $"Tuple element {ToOrdinal(i + 1)} must be numeric, range, or tuple, " +
                        $"but expression evaluated to '{v.type}'.",
                        item.Token);
                }

                switch (v.type)
                {
                    case EDataType.Real:
                    case EDataType.Integer:
                    case EDataType.Text:
                    case EDataType.Range:
                    case EDataType.Tuple:
                        values.Add(v); 
                        break;

                    default:
                        throw new RunTimeException(
                            $"Tuple element {ToOrdinal(i + 1)} must be numeric, range, or tuple, " +
                            $"but expression evaluated to '{v.type}'.",
                            item.Token);
                }
            }

            if (allCompileTimeKnown)
            {
                _value = new TuValue(values);
                return _value;
            }
            return new TuValue(values);
        }
    }
}
