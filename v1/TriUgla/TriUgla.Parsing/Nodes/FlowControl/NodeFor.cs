using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeFor : NodeBase
    {
        public NodeFor(Token start, NodeIdentifier id, NodeBase range, NodeBlock block, Token end) : base(start)
        {
            Counter = id;
            Range = range;
            Block = block;
            End = end;
        }

        public Token Start => Token;
        public Token End { get; }

        public NodeIdentifier Counter { get; }
        public NodeBase Range { get; }
        public NodeBlock Block { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            Variable i = stack.Current.GetOrDeclare(Counter.Name);

            TuValue list = Range.Evaluate(stack);

            switch (list.type)
            {
                case EDataType.Range:
                    foreach (double item in list.AsRange()!)
                    {
                        i.Value = new TuValue(item);
                        Block.Evaluate(stack);
                    }
                    break;
                case EDataType.Tuple:
                    foreach (double item in list.AsTuple()!)
                    {
                        i.Value = new TuValue(item);
                        Block.Evaluate(stack);
                    }
                    break;
                default:
                    throw new Exception($"For-loop expects {EDataType.Range} or {EDataType.Tuple} but got {list.type}.");
            }

            return i.Value;
        }
    }
}
