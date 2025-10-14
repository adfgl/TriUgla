using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeRange : NodeLiteralBase
    {
        public NodeRange(Token start, IEnumerable<NodeBase> args, Token end) : base(start)
        {
            Args = args.ToArray();
            End = end;  
        }

        public IReadOnlyList<NodeBase> Args { get; }

        public Token Start => Token;
        public Token End { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            if (Args.Count != 2 && Args.Count != 3)
                throw new CompiletimeException(
                    $"Range expects 2 or 3 arguments: Range(from, to[, by]). Got {Args.Count}.",
                    Token);

            TuValue fromV = Args[0].Evaluate(stack);
            TuValue toV = Args[1].Evaluate(stack);
            TuValue byV = Args.Count == 3 ? Args[2].Evaluate(stack) : new TuValue(1);

            if (fromV.type != EDataType.Numeric)
                throw new RuntimeException("Range 'from' must be numeric.", Args[0].Token);

            if (toV.type != EDataType.Numeric)
                throw new RuntimeException("Range 'to' must be numeric.", Args[1].Token);

            if (byV.type != EDataType.Numeric)
                throw new RuntimeException("Range 'by' must be numeric.", Args.Count == 3 ? Args[2].Token : Token);

            double f = fromV.AsNumeric();
            double t = toV.AsNumeric();
            double b = byV.AsNumeric();
            if (b == 0)
            {
                throw new RuntimeException("Range step 'by' cannot be zero.", Args.Count == 3 ? Args[2].Token : Token);
            }

            bool ascending = t >= f;
            bool stepUp = b > 0;
            if (ascending && !stepUp)
            {
                throw new RuntimeException(
                    $"Range step {b} does not progress from {f} to {t}. Use a positive step.",
                    Args.Count == 3 ? Args[2].Token : Token);
            }

            if (!ascending && stepUp)
            {
                throw new RuntimeException(
                    $"Range step {b} does not progress from {f} to {t}. Use a negative step.",
                    Args.Count == 3 ? Args[2].Token : Token);
            }
            return new TuValue(new TuRange(f, t, b));
        }

        public override string ToString()
        {
            return "";
        }
    }
}
