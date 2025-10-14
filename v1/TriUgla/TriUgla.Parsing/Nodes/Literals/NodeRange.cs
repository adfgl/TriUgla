using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeRange : NodeBase
    {
        public NodeRange(Token start, NodeBase from, NodeBase to, NodeBase? by, Token end) : base(start)
        {
            From = from;
            To = to;
            By = by;
            End = end;  
        }

        public NodeBase From { get; }
        public NodeBase To { get; }
        public NodeBase? By { get; }

        public Token Start => Token;
        public Token End { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue from = From.Evaluate(stack);
            if (from.type != EDataType.Numeric)
            {
                throw new Exception("Range 'from' must be numeric");
            }

            TuValue to = To.Evaluate(stack);
            if (to.type != EDataType.Numeric)
            {
                throw new Exception("Range 'to' must be numeric");
            }

            TuValue by = By is null ? new TuValue(1) : By.Evaluate(stack);
            if (by.type != EDataType.Numeric)
            {
                throw new Exception("Range 'by' must be numeric");
            }

            double f = from.AsNumeric();
            double t = to.AsNumeric();
            double b = by.AsNumeric();

            if (b == 0)
            {
                throw new Exception("Range step cannot be zero");
            }
            if ((t - f) / b < 0)
            {
                throw new Exception("Range direction and step mismatch");
            }
            return new TuValue(new TuRange(f, t, b));
        }

        public override string ToString()
        {
            return $"{Start.type} {From} {To} {(By is null ? "" : By)} {End.type}";
        }
    }
}
