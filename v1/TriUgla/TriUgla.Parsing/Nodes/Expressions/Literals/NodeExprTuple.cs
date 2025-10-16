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

            TuTuple values = new TuTuple(Args.Count);

            bool allCompileTimeKnown = true;
            EDataType type = EDataType.Nothing;
            for (int i = 0; i < Args.Count; i++)
            {
                NodeBase arg = Args[i];
                if (allCompileTimeKnown && arg is not NodeExprLiteralBase)
                {
                    allCompileTimeKnown = false;
                }

                foreach (TuValue v in arg.Evaluate(rt).AsTuple())
                {
                    TuValue set = v;
                    if (v.type == type || type == EDataType.Nothing)
                    {
                        type = v.type;
                    }
                    else if (type == EDataType.Real && v.type == EDataType.Integer)
                    {
                        set = new TuValue(v.AsNumeric());
                    }
                    else if (type == EDataType.Integer && v.type == EDataType.Real)
                    {
                        type = EDataType.Real;
                        set = new TuValue(v.AsNumeric());
                        for (int j = 0; j < i; j++)
                        {
                            values[j] = new TuValue(values[j].AsNumeric());
                        }
                    }
                    else
                    {
                        throw new RunTimeException(
                            $"Type mismatch — expected: {type}; actual: {v.type}.",
                            arg.Token);
                    }

                    values.Add(set);
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
