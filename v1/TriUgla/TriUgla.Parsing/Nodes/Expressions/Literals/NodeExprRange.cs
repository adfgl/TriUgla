using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions.Literals
{
    public sealed class NodeExprRange : NodeExprLiteralBase
    {
        TuValue _value = TuValue.Nothing;

        public NodeExprRange(Token start, IEnumerable<NodeBase> args, Token end) : base(start)
        {
            Args = args.ToArray();
            End = end;  
        }

        public IReadOnlyList<NodeBase> Args { get; }

        public Token Start => Token;
        public Token End { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            if (_value.type != EDataType.Nothing)
            {
                return _value;
            }

            if (Args.Count != 2 && Args.Count != 3)
            {
                throw new CompileTimeException(
                    $"Range expects 2 or 3 arguments: Range(from, to, by). Got {Args.Count}.",
                    Token);
            }

            bool allCompileTimeKnown = true;
            TuValue[] values = new TuValue[3];
            values[2] = new TuValue(1);
            for (int i = 0; i < Args.Count; i++)
            {
                NodeBase arg = Args[i];
                if (allCompileTimeKnown && arg is not NodeExprLiteralBase)
                {
                    allCompileTimeKnown = false;
                }

                TuValue v = arg.Evaluate(rt);

                string argStr = i switch
                {
                    0 => "from",
                    1 => "to",
                    _ => "by"
                };

                if (!v.type.IsNumeric())
                {
                    string msg = $"Range '{argStr}' must be numeric.";
                    if (arg is NodeExprLiteralBase)
                    {
                        throw new CompileTimeException(msg, arg.Token);
                    }
                    else
                    {
                        throw new RunTimeException(msg, arg.Token);
                    }
                }

                values[i] = v;
            }

            double f = values[0].AsNumeric();
            double t = values[1].AsNumeric();
            double b = values[2].AsNumeric();
            if (b == 0)
            {
                throw new RunTimeException("Range step 'by' cannot be zero.", Args.Count == 3 ? Args[2].Token : Token);
            }

            bool ascending = t >= f;
            bool stepUp = b > 0;
            if (ascending && !stepUp)
            {
                throw new RunTimeException(
                    $"Range step {b} does not progress from {f} to {t}. Use a positive step.",
                    Args.Count == 3 ? Args[2].Token : Token);
            }

            if (!ascending && stepUp)
            {
                throw new RunTimeException(
                    $"Range step {b} does not progress from {f} to {t}. Use a negative step.",
                    Args.Count == 3 ? Args[2].Token : Token);
            }

            TuRange rng = new TuRange(values[0], values[1], values[2]);
            if (allCompileTimeKnown)
            {
                _value = new TuValue(rng);
                return _value;
            }
            return new TuValue(rng);
        }

        public override string ToString()
        {
            return "";
        }
    }
}
