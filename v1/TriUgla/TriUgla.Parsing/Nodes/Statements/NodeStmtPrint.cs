using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public class NodeStmtPrint : NodeStmtBase
    {
        public NodeStmtPrint(Token token, NodeBase? arg) : base(token)
        {
            Arg = arg;
        }

        public NodeBase? Arg { get; set; }

        protected override TuValue Evaluate(TuRuntime stack)
        {
            string msg = string.Empty;
            if (Arg != null)
            {
                TuValue v = Arg.Eval(stack);
                msg = v.AsString();
            }
            stack.Print(Token, msg);
            return new TuValue(msg);
        }
    }
}
