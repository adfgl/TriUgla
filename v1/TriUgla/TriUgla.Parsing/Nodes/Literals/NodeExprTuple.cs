using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeExprTuple : NodeExprLiteralBase
    {
        public NodeExprTuple(Token open, IEnumerable<NodeBase> args, Token close) : base(open)
        {
            Args = args.ToArray();
            Close = close;
        }

        public IReadOnlyList<NodeBase> Args { get; }
        public Token Open => Token;
        public Token Close { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            List<double> values = new List<double>(Args.Count);
            for (int i = 0; i < Args.Count; i++)
            {
                NodeBase item = Args[i];
                TuValue v = item.Evaluate(stack);

                switch (v.type)
                {
                    case EDataType.Numeric:
                        values.Add(v.AsNumeric());
                        break;

                    case EDataType.Range:
                    case EDataType.Tuple:
                        var tpl = v.AsTuple()!;
                        values.AddRange(tpl); // flattens nested range/tuple
                        break;

                    default:
                        throw new RunTimeException(
                            $"Tuple element {ToOrdinal(i + 1)} must be numeric, range, or tuple, " +
                            $"but expression evaluated to '{v.type}'.",
                            item.Token);
                }
            }
            return new TuValue(new TuTuple(values));
        }
    }
}
