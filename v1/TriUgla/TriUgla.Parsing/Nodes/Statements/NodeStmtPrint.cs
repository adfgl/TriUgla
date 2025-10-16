using TriUgla.Parsing.Data;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Statements
{
    public sealed class NodeStmtPrint : NodeStmtBase
    {
        public NodeStmtPrint(Token token, NodeBase? arg) : base(token)
        {
            Arg = arg;
        }

        public NodeBase? Arg { get; set; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            string msg = string.Empty;
            if (Arg != null)
            {
                TuValue v = Arg.Evaluate(rt);
                msg = v.AsString();
            }
            rt.Print(Token, msg);
            return new TuValue(msg);
        }
    }
}
