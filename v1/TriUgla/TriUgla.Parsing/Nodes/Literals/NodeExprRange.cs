using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeExprRange : NodeExprLiteralBase
    {
        public NodeExprRange(Token start, IEnumerable<NodeBase> args, Token end) : base(start)
        {
            Args = args.ToArray();
            End = end;  
        }

        public IReadOnlyList<NodeBase> Args { get; }

        public Token Start => Token;
        public Token End { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            if (Args.Count != 2 && Args.Count != 3)
            {
                throw new CompileTimeException(
                    $"Range expects 2 or 3 arguments: Range(from, to, by). Got {Args.Count}.",
                    Token);
            }

            TuValue[] values = new TuValue[3];
            values[2] = new TuValue(1);
            for (int i = 0; i < Args.Count; i++)
            {
                NodeBase arg = Args[i];
                TuValue v = arg.Evaluate(stack);

                string argStr = i switch
                {
                    0 => "from",
                    1 => "to",
                    _ => "by"
                };

                if (v.type != EDataType.Numeric)
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
            Value = new TuValue(new TuRange(f, t, b));
            return Value;
        }

        public override string ToString()
        {
            return "";
        }
    }
}
