using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.FlowControl
{
    public class NodeStmtFor : NodeBase
    {
        public NodeStmtFor(Token start, NodeBase id, NodeBase range, NodeStmtBlock block, Token end) : base(start)
        {
            Counter = id;
            Range = range;
            Block = block;
            End = end;
        }

        public Token Start => Token;
        public Token End { get; }

        public NodeBase Counter { get; }
        public NodeBase Range { get; }
        public NodeStmtBlock Block { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            NodeExprIdentifier? id = Counter as NodeExprIdentifier;
            if (id is null)
            {
                throw new CompileTimeException($"Expected identifier but got '{Counter.Token.type}'.", Counter.Token);
            }

            id.DeclareIfMissing = true;
            id.Evaluate(stack);
            Variable counter = stack.Current.Get(id.Name)!;

            TuValue list = Range.Evaluate(stack);
            switch (list.type)
            {
                case EDataType.Range:
                    foreach (double item in list.AsRange()!)
                    {
                        counter.Value = new TuValue(item);
                        Block.Evaluate(stack);
                    }
                    break;
                case EDataType.Tuple:
                    foreach (double item in list.AsTuple()!)
                    {
                        counter.Value = new TuValue(item);
                        Block.Evaluate(stack);
                    }
                    break;
                default:
                    throw new Exception($"For-loop expects {EDataType.Range} or {EDataType.Tuple} but got {list.type}.");
            }
            return TuValue.Nothing;
        }
    }
}
