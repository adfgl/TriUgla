using TriUgla.Parsing.Data;
using TriUgla.Parsing.Nodes.Statements;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprProgram : NodeExprBase
    {
        public NodeExprProgram(Token token, NodeStmtBlock block) : base(token)
        {
            Statements = block;
        }

        public NodeStmtBlock Statements { get; }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            rt.OpenScope();
            rt.Global.Declare(new Token(ETokenType.Identifier, -1, -1, "Pi"), new TuValue(3.1415926535897932));

            return Statements.Evaluate(rt);
        }
    }
}
