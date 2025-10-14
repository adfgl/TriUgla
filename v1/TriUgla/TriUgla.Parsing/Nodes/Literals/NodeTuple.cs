using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Literals
{
    public class NodeTuple : NodeLiteralBase
    {

        public NodeTuple(Token open, IEnumerable<NodeBase> args, Token close) : base(open)
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
            foreach (NodeBase item in Args)
            {
                TuValue v = item.Evaluate(stack);
                switch (v.type)
                {
                    case EDataType.Numeric:
                        values.Add(v.AsNumeric());
                        break;
                    case EDataType.Range:
                    case EDataType.Tuple:
                        TuTuple tpl = v.AsTuple()!;
                        values.AddRange(tpl);
                        break;
                    default:
                        throw new Exception("Tuple elements must be numeric");
                }
            }
            return new TuValue(new TuTuple(values));
        }
    }
}
