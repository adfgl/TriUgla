using TriScript.Data;
using TriScript.Parsing.Nodes.Statements;

namespace TriScript.Parsing.Nodes.Expressions
{
    public class NodeExprProgram : NodeExprBase
    {
        public NodeExprProgram(NodeStmtBlock body)
        {
            Body = body;
        }

        public NodeStmtBlock Body { get; }

        public override Value Evaluate(Executor rt)
        {
            Body.Evaluate(rt);
            return Value.Nothing;
        }
    }
}
