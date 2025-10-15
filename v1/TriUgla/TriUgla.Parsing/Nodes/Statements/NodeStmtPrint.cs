using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtPrint : NodeStmtBase, IParsableNode<NodeStmtPrint>
    {
        public NodeStmtPrint(Token token, NodeBase? arg) : base(token)
        {
            Arg = arg;
        }

        public NodeBase? Arg { get; set; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            string msg = string.Empty;
            if (Arg != null)
            {
                TuValue v = Arg.Evaluate(stack);
                msg = v.AsString();
            }
            stack.Print(Token, msg);
            return TuValue.Nothing;
        }

        public static NodeStmtPrint Parse(Parser p)
        {
            Token print = p.Consume(ETokenType.Print);
            p.Consume(ETokenType.OpenParen);

            NodeBase? expr = null;
            if (!p.TryConsume(ETokenType.CloseParen, out _))
            {
                expr = p.ParseExpression();
                p.Consume(ETokenType.CloseParen);
            }
            p.MaybeEOX();
            return new NodeStmtPrint(print, expr);
        }
    }
}
